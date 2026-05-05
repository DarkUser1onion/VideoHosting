using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VideoHostingByWhoami.Services;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Model;

public class PlaylistViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    
    public ObservableCollection<Playlist> Playlists { get; } = new();
    
    public SimpleCommand CreatePlaylistCommand { get; }
    public SimpleCommand<Guid> DeletePlaylistCommand { get; }
    public SimpleCommand<Guid> RemoveFromPlaylistCommand { get; }
    public SimpleCommand<VideoItem> OpenVideoCommand { get; }
    
    public PlaylistViewModel(IApiService api)
    {
        _api = api;
        
        CreatePlaylistCommand = new SimpleCommand(async () => await CreatePlaylist());
        DeletePlaylistCommand = new SimpleCommand<Guid>(async id => await DeletePlaylist(id));
        RemoveFromPlaylistCommand = new SimpleCommand<Guid>(async id => await RemoveFromPlaylist(id));
        OpenVideoCommand = new SimpleCommand<VideoItem>(video => { if (video != null) OpenVideo(video); });
        
        _ = LoadPlaylists();
    }
    
    private async Task LoadPlaylists()
    {
        var playlists = await _api.GetPlaylistsAsync();
        Playlists.Clear();
        foreach (var playlist in playlists)
        {
            Playlists.Add(playlist);
        }
    }
    
    private async Task CreatePlaylist()
    {
        // Show input dialog
        var inputWindow = new InputDialogWindow("Название плейлиста");
        var result = await inputWindow.ShowDialog();
        
        if (!string.IsNullOrEmpty(result))
        {
            var success = await _api.CreatePlaylistAsync(result);
            if (success)
            {
                await LoadPlaylists();
            }
        }
    }
    
    private async Task DeletePlaylist(Guid playlistId)
    {
        var success = await _api.DeletePlaylistAsync(playlistId);
        if (success)
        {
            await LoadPlaylists();
        }
    }
    
    private async Task RemoveFromPlaylist(Guid videoId)
    {
        // Find the playlist containing this video
        foreach (var playlist in Playlists)
        {
            var video = playlist.Videos.FirstOrDefault(v => v.Id == videoId);
            if (video != null)
            {
                var success = await _api.RemoveVideoFromPlaylistAsync(playlist.Id, videoId);
                if (success)
                {
                    await LoadPlaylists();
                    return;
                }
            }
        }
    }
    
    private void OpenVideo(VideoItem video)
    {
        var playerWindow = new PlayerWindow(_api, video);
        playerWindow.Show();
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
