using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VideoHostingByWhoami.Services;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Model;

public class PlayerViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    
    public VideoItem Video { get; }
    public string VideoTitle => Video.Title;
    public string VideoStreamUrl => $"http://localhost:5000/api/videos/{Video.Id}/stream";
    
    public ObservableCollection<Comment> Comments { get; } = new();
    
    private string _newComment = string.Empty;
    public string NewComment
    {
        get => _newComment;
        set { _newComment = value; OnPropertyChanged(); }
    }
    
    private bool _isLoading = true;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }
    
    private bool? _userLiked;
    public bool? UserLiked
    {
        get => _userLiked;
        set { _userLiked = value; OnPropertyChanged(); }
    }
    
    public SimpleCommand LikeCommand { get; }
    public SimpleCommand DislikeCommand { get; }
    public SimpleCommand SendCommentCommand { get; }
    public SimpleCommand AddToPlaylistCommand { get; }
    public SimpleCommand OpenExternalCommand { get; }
    
    public PlayerViewModel(IApiService api, VideoItem video)
    {
        _api = api;
        Video = video;
        
        LikeCommand = new SimpleCommand(async () => await SetLike(true));
        DislikeCommand = new SimpleCommand(async () => await SetLike(false));
        SendCommentCommand = new SimpleCommand(async () => await SendComment());
        AddToPlaylistCommand = new SimpleCommand(ShowPlaylistSelector);
        OpenExternalCommand = new SimpleCommand(OpenExternal);
        
        _ = LoadData();
    }
    
    private void OpenExternal()
    {
        // Open video in system player
        try
        {
            var url = $"http://localhost:5000/api/videos/{Video.Id}/stream";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }
    
    private async Task LoadData()
    {
        await Task.WhenAll(LoadComments(), LoadLikeStatus());
        IsLoading = false;
    }
    
    private async Task LoadComments()
    {
        var comments = await _api.GetCommentsAsync(Video.Id);
        Comments.Clear();
        foreach (var comment in comments)
        {
            Comments.Add(comment);
        }
    }
    
    private async Task LoadLikeStatus()
    {
        if (_api.IsAuthenticated)
        {
            UserLiked = await _api.GetLikeStatusAsync(Video.Id);
        }
    }
    
    private async Task SetLike(bool isLike)
    {
        if (!_api.IsAuthenticated) return;
        
        var success = await _api.SetLikeAsync(Video.Id, isLike);
        if (success)
        {
            UserLiked = isLike;
            
            // Update local counts
            if (isLike)
                Video.Likes++;
            else
                Video.Dislikes++;
            
            OnPropertyChanged(nameof(Video));
        }
    }
    
    private async Task SendComment()
    {
        if (!_api.IsAuthenticated || string.IsNullOrWhiteSpace(NewComment)) return;
        
        var success = await _api.AddCommentAsync(Video.Id, NewComment);
        if (success)
        {
            NewComment = "";
            await LoadComments();
        }
    }
    
    private void ShowPlaylistSelector()
    {
        var selector = new PlaylistSelectorWindow(_api, Video.Id);
        selector.Show();
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
