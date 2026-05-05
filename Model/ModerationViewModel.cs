using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using VideoHostingByWhoami.Services;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Model;

public class ModerationViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    private bool _isAdmin;
    
    public ObservableCollection<VideoItem> PendingVideos { get; } = new();
    public ObservableCollection<UserItem> Users { get; } = new();
    
    public bool IsAdmin => _isAdmin;
    public bool CanDeleteUsers => _isAdmin;
    
    public SimpleCommand<Guid> ApproveCommand { get; }
    public SimpleCommand<Guid> RejectCommand { get; }
    public SimpleCommand<Guid> PreviewCommand { get; }
    public SimpleCommand<Guid> DeleteUserCommand { get; }
    
    public ModerationViewModel(IApiService api)
    {
        _api = api;
        
        // Проверяем роль пользователя (админ или модератор)
        _isAdmin = true; // Временно, потом нужно проверять через API
        
        ApproveCommand = new SimpleCommand<Guid>(async id => await Moderate(id, true));
        RejectCommand = new SimpleCommand<Guid>(async id => await Moderate(id, false));
        PreviewCommand = new SimpleCommand<Guid>(PreviewVideo);
        DeleteUserCommand = new SimpleCommand<Guid>(async id => await DeleteUser(id));
        
        _ = LoadData();
    }
    
    private async Task LoadData()
    {
        await Task.WhenAll(LoadPendingVideos(), LoadUsers());
    }
    
    private async Task LoadPendingVideos()
    {
        try
        {
            var videos = await _api.GetPendingVideosAsync();
            PendingVideos.Clear();
            foreach (var video in videos)
            {
                video.PreviewUrl = $"http://localhost:5000/api/videos/{video.Id}/preview";
                try
                {
                    var previewBytes = await _api.GetPreviewAsync(video.Id);
                    if (previewBytes.Length > 0)
                    {
                        using var stream = new MemoryStream(previewBytes);
                        video.PreviewImage = new Bitmap(stream);
                    }
                }
                catch
                {
                    video.PreviewImage = null;
                }

                PendingVideos.Add(video);
            }
        }
        catch
        {
            // Keep UI responsive if backend call fails
        }
    }
    
    private async Task LoadUsers()
    {
        try
        {
            var users = await _api.GetAllUsersAsync();
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }
        catch
        {
            // Keep UI responsive if backend call fails
        }
    }
    
    private async Task Moderate(Guid videoId, bool approve)
    {
        string? reason = null;
        
        if (!approve)
        {
            var inputWindow = new InputDialogWindow("Причина отклонения");
            reason = await inputWindow.ShowDialog();
        }
        
        var success = await _api.ModerateVideoAsync(videoId, approve, reason);
        if (success)
        {
            await LoadPendingVideos();
        }
    }

    private void PreviewVideo(Guid videoId)
    {
        var video = FindVideo(videoId);
        if (video == null) return;

        var playerWindow = new PlayerWindow(_api, video, true, LoadPendingVideos);
        playerWindow.Show();
    }
    
    private async Task DeleteUser(Guid userId)
    {
        var success = await _api.DeleteUserAsync(userId);
        if (success)
        {
            await LoadUsers();
        }
    }

    private VideoItem? FindVideo(Guid videoId)
    {
        foreach (var video in PendingVideos)
        {
            if (video.Id == videoId)
                return video;
        }
        return null;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
