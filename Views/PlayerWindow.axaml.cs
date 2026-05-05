using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Views;

public partial class PlayerWindow : Window
{
    private PlayerViewModel? _viewModel;
    private Process? _mpvProcess;
    
    public PlayerWindow()
    {
        InitializeComponent();
    }
    
    public PlayerWindow(IApiService api, VideoItem video, bool canDeleteVideo = false, Func<Task>? refreshCallback = null)
    {
        InitializeComponent();
        _viewModel = new PlayerViewModel(api, video, this, canDeleteVideo, refreshCallback);
        DataContext = _viewModel;
        
        // Подписываемся на команды открытия в mpv/browser из ViewModel
        _viewModel.OpenExternalRequested += OnOpenExternalRequested;
        _viewModel.OpenBrowserRequested += OnOpenBrowserRequested;
    }
    
    private bool IsMpvRunning => _mpvProcess != null && !_mpvProcess.HasExited;
    
    private void OnOpenExternalRequested()
    {
        if (_viewModel == null || IsMpvRunning) return;
        
        var streamUrl = $"http://localhost:5000/api/videos/{_viewModel.Video.Id}/stream";
        
        try
        {
            // Записываем просмотр при открытии MPV
            _ = RecordViewAsync(_viewModel.Video.Id);
            
            _mpvProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "mpv",
                Arguments = $"--title=\"{_viewModel.Video.Title}\" \"{streamUrl}\"",
                UseShellExecute = false,
                CreateNoWindow = false
            });
            
            if (_mpvProcess != null)
            {
                _mpvProcess.Exited += (s, e) =>
                {
                    _mpvProcess = null;
                };
                _mpvProcess.EnableRaisingEvents = true;
            }
        }
        catch
        {
            // Fallback - открываем в браузере
            OnOpenBrowserRequested();
        }
    }
    
    private static async Task RecordViewAsync(Guid videoId)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            await client.PostAsync($"http://localhost:5000/api/videos/{videoId}/view", null);
        }
        catch
        {
            // Игнорируем ошибки записи просмотра
        }
    }
    
    private void OnOpenBrowserRequested()
    {
        if (_viewModel == null) return;
        
        var watchUrl = $"http://localhost:5000/api/videos/{_viewModel.Video.Id}/watch";
        
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = watchUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        // Останавливаем mpv при закрытии окна
        if (_mpvProcess != null && !_mpvProcess.HasExited)
        {
            _mpvProcess.Kill();
            _mpvProcess.Dispose();
            _mpvProcess = null;
        }
        
        _viewModel?.OnClosed();
        base.OnClosed(e);
    }
}
