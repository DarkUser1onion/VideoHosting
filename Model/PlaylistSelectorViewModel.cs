using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using VideoHostingByWhoami.Services;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Model;

public class PlaylistSelectorViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    private readonly Guid _videoId;
    private readonly Window _window;
    
    public ObservableCollection<Playlist> Playlists { get; } = new();
    
    public SimpleCommand<Guid> SelectPlaylistCommand { get; }
    public SimpleCommand CreatePlaylistCommand { get; }
    
    public PlaylistSelectorViewModel(IApiService api, Guid videoId, Window window)
    {
        _api = api;
        _videoId = videoId;
        _window = window;
        
        SelectPlaylistCommand = new SimpleCommand<Guid>(async id => await SelectPlaylist(id));
        CreatePlaylistCommand = new SimpleCommand(async () => await CreateAndAdd());
        
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
    
    private async Task SelectPlaylist(Guid playlistId)
    {
        var success = await _api.AddVideoToPlaylistAsync(playlistId, _videoId);
        if (success)
        {
            _window.Close();
        }
    }
    
    private async Task CreateAndAdd()
    {
        var inputWindow = new InputDialogWindow("Название плейлиста");
        var result = await inputWindow.ShowDialog();
        
        if (!string.IsNullOrEmpty(result))
        {
            var createSuccess = await _api.CreatePlaylistAsync(result);
            if (createSuccess)
            {
                await LoadPlaylists();
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
