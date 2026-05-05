using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Views;

public partial class PlayerWindow : Window
{
    private PlayerViewModel? _viewModel;
    private Process? _mpvProcess;
    private IntPtr _windowHandle;
    
    public PlayerWindow()
    {
        InitializeComponent();
    }
    
    public PlayerWindow(IApiService api, VideoItem video, bool canDeleteVideo = false, Func<Task>? refreshCallback = null)
    {
        InitializeComponent();
        _viewModel = new PlayerViewModel(api, video, this, canDeleteVideo, refreshCallback);
        DataContext = _viewModel;
        
        // Инициализируем плеер после открытия окна
        Opened += OnWindowOpened;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        if (_viewModel == null) return;
        
        // Получаем handle окна для встраивания mpv
        var platformHandle = TryGetPlatformHandle();
        if (platformHandle != null)
        {
            _windowHandle = platformHandle.Handle;
            StartMpvPlayer();
        }
    }
    
    private void StartMpvPlayer()
    {
        if (_viewModel == null) return;
        
        var streamUrl = $"http://localhost:5000/api/videos/{_viewModel.Video.Id}/stream";
        
        try
        {
            // На Linux используем X11 window ID для встраивания mpv
            var psi = new ProcessStartInfo
            {
                FileName = "mpv",
                Arguments = $"--wid={_windowHandle} " +
                           $"--no-border " +
                           $"--keepaspect=yes " +
                           $"--autofit=100% " +
                           $"--input-ipc-server=/tmp/mpv-{Guid.NewGuid():N}.sock " +
                           $"\"{streamUrl}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            _mpvProcess = Process.Start(psi);
            
            if (_mpvProcess != null)
            {
                _viewModel.IsPlayerReady = true;
                
                // Обрабатываем завершение процесса
                _mpvProcess.Exited += (s, e) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _viewModel.IsPlayerReady = false;
                    });
                };
                _mpvProcess.EnableRaisingEvents = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start mpv: {ex.Message}");
            // Fallback - открываем в браузере
            OpenInBrowser(streamUrl);
        }
    }
    
    private void OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
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
        }
        
        _viewModel?.OnClosed();
        base.OnClosed(e);
    }
}
