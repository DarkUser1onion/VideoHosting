using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VideoHostingByWhoami.Services;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Model;

public class ModerationViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    
    public ObservableCollection<VideoItem> PendingVideos { get; } = new();
    
    public SimpleCommand<Guid> ApproveCommand { get; }
    public SimpleCommand<Guid> RejectCommand { get; }
    
    public ModerationViewModel(IApiService api)
    {
        _api = api;
        
        ApproveCommand = new SimpleCommand<Guid>(async id => await Moderate(id, true));
        RejectCommand = new SimpleCommand<Guid>(async id => await Moderate(id, false));
        
        _ = LoadPendingVideos();
    }
    
    private async Task LoadPendingVideos()
    {
        var videos = await _api.GetPendingVideosAsync();
        PendingVideos.Clear();
        foreach (var video in videos)
        {
            video.PreviewUrl = $"http://localhost:5000/api/videos/{video.Id}/preview";
            PendingVideos.Add(video);
        }
    }
    
    private async Task Moderate(Guid videoId, bool approve)
    {
        string? reason = null;
        
        if (!approve)
        {
            // Show input dialog for rejection reason
            var inputWindow = new InputDialogWindow("Причина отклонения");
            reason = await inputWindow.ShowDialog();
        }
        
        var success = await _api.ModerateVideoAsync(videoId, approve, reason);
        if (success)
        {
            await LoadPendingVideos();
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
