using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using VideoHostingByWhoami.Services;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Model;

public class PlayerViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    private readonly Window? _ownerWindow;
    private readonly Func<Task>? _refreshCallback;

    public VideoItem Video { get; }
    public string VideoTitle => Video.Title;
    public bool CanDeleteVideo { get; }

    public string StreamUrl => $"http://localhost:5000/api/videos/{Video.Id}/stream";

    private bool _isPlayerReady;
    public bool IsPlayerReady
    {
        get => _isPlayerReady;
        set { _isPlayerReady = value; OnPropertyChanged(); }
    }

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

    private string _playbackError = string.Empty;
    public string PlaybackError
    {
        get => _playbackError;
        set
        {
            _playbackError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPlaybackError));
            OnPropertyChanged(nameof(IsPlaybackAvailable));
        }
    }

    public bool HasPlaybackError => !string.IsNullOrWhiteSpace(PlaybackError);
    public bool IsPlaybackAvailable => !HasPlaybackError;

    private bool? _userLiked;
    public bool? UserLiked
    {
        get => _userLiked;
        set
        {
            Console.WriteLine($"UserLiked changed from {_userLiked} to {value}");
            _userLiked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLikeActive));
            OnPropertyChanged(nameof(IsDislikeActive));
            OnPropertyChanged(nameof(LikeButtonClasses));
            OnPropertyChanged(nameof(DislikeButtonClasses));
        }
    }
    
    public string LikeButtonClasses => IsLikeActive ? "like active" : "like";
    public string DislikeButtonClasses => IsDislikeActive ? "like active" : "like";
    
    public bool IsLikeActive => UserLiked == true;
    public bool IsDislikeActive => UserLiked == false;

    public SimpleCommand LikeCommand { get; }
    public SimpleCommand DislikeCommand { get; }
    public SimpleCommand SendCommentCommand { get; }
    public SimpleCommand AddToPlaylistCommand { get; }
    public SimpleCommand DeleteVideoCommand { get; }
    public SimpleCommand<Guid> DeleteCommentCommand { get; }
    public SimpleCommand OpenExternalCommand { get; }
    public SimpleCommand OpenInVlcCommand { get; }
    public SimpleCommand OpenInBrowserCommand { get; }
    
    private bool _isModeratorOrAdmin = false;
    public bool CanDeleteComments => _isModeratorOrAdmin;

    public PlayerViewModel(IApiService api, VideoItem video, Window? ownerWindow = null, bool canDeleteVideo = false, Func<Task>? refreshCallback = null)
    {
        _api = api;
        Video = video;
        _ownerWindow = ownerWindow;
        CanDeleteVideo = canDeleteVideo;
        _refreshCallback = refreshCallback;

        // Проверяем роль пользователя
        if (_api.IsAuthenticated && _api.CurrentUser != null)
        {
            var role = _api.CurrentUser.Role;
            _isModeratorOrAdmin = role == "moderator" || role == "admin";
        }

        LikeCommand = new SimpleCommand(async () => await SetLike(true));
        DislikeCommand = new SimpleCommand(async () => await SetLike(false));
        SendCommentCommand = new SimpleCommand(async () => await SendComment());
        AddToPlaylistCommand = new SimpleCommand(ShowPlaylistSelector);
        DeleteVideoCommand = new SimpleCommand(async () => await DeleteVideo());
        DeleteCommentCommand = new SimpleCommand<Guid>(async id => await DeleteComment(id));
        OpenExternalCommand = new SimpleCommand(OpenInExternalPlayer);
        OpenInVlcCommand = new SimpleCommand(OpenInVlc);
        OpenInBrowserCommand = new SimpleCommand(OpenInBrowser);

        _ = LoadData();
    }

    public void DisposePlayer()
    {
        // Освобождение ресурсов плеера
        // Для mpv это делается в PlayerWindow.axaml.cs
    }
    
    public void TryAutoOpenWatchInBrowserOnce()
    {
        // Автооткрытие в браузере для встроенного плеера
        // Сейчас не используется, так как используем mpv
    }

    public async void OnClosed()
    {
        if (_refreshCallback != null)
        {
            try
            {
                await _refreshCallback();
            }
            catch
            {
                // Ignore refresh errors
            }
        }
    }

    private async Task DeleteVideo()
    {
        if (!CanDeleteVideo || _ownerWindow == null) return;

        var confirmWindow = new DeleteConfirmWindow();
        var confirmed = await confirmWindow.ShowConfirmDialog(_ownerWindow);
        if (!confirmed) return;

        var deleted = await _api.DeleteVideoAsync(Video.Id);
        if (!deleted) return;

        if (_refreshCallback != null)
            await _refreshCallback();

        _ownerWindow.Close();
    }

    private async Task LoadData()
    {
        // Загружаем актуальные данные о видео с сервера
        var updatedVideo = await _api.GetVideoAsync(Video.Id);
        if (updatedVideo != null)
        {
            Video.Likes = updatedVideo.Likes;
            Video.Dislikes = updatedVideo.Dislikes;
            Video.Views = updatedVideo.Views;
            Video.Duration = updatedVideo.Duration;
            OnPropertyChanged(nameof(Video));
        }
        
        await Task.WhenAll(LoadComments(), LoadLikeStatus());
        IsLoading = false;
    }
    
    private async Task DeleteComment(Guid commentId)
    {
        if (!_isModeratorOrAdmin) return;
        
        var success = await _api.DeleteCommentAsync(commentId);
        if (success)
        {
            await LoadComments();
        }
    }

    private async Task LoadComments()
    {
        var comments = await _api.GetCommentsAsync(Video.Id);
        Comments.Clear();
        foreach (var comment in comments)
            Comments.Add(comment);
    }

    private async Task LoadLikeStatus()
    {
        if (_api.IsAuthenticated)
            UserLiked = await _api.GetLikeStatusAsync(Video.Id);
    }

    private async Task SetLike(bool isLike)
    {
        PlaybackError = string.Empty;
        if (!_api.IsAuthenticated)
        {
            PlaybackError = "Нужно войти в аккаунт, чтобы ставить лайки";
            return;
        }

        var success = await _api.SetLikeAsync(Video.Id, isLike);
        if (success)
        {
            // Перезапрашиваем актуальный статус лайка (true/false/null)
            UserLiked = await _api.GetLikeStatusAsync(Video.Id);
            // Обновляем числа лайков/дизлайков с сервера
            var updated = await _api.GetVideoAsync(Video.Id);
            if (updated != null)
            {
                Video.Likes = updated.Likes;
                Video.Dislikes = updated.Dislikes;
                Video.Views = updated.Views;
                // Явно уведомляем об изменении свойств
                OnPropertyChanged(nameof(Video));
            }
        }
        else
        {
            PlaybackError = "Не удалось изменить лайк";
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
    
    private void OpenInExternalPlayer()
    {
        OpenInVlc();
    }
    
    private void OpenInVlc()
    {
        try
        {
            var streamUrl = StreamUrl;
            
            // Пробуем VLC
            try
            {
                using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "vlc",
                    Arguments = $"\"{streamUrl}\"",
                    UseShellExecute = false,
                    CreateNoWindow = false
                });
                return;
            }
            catch
            {
                // Если VLC не установлен, пробуем mpv
                try
                {
                    using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "mpv",
                        Arguments = $"\"{streamUrl}\" --title=\"VideoHosting Player\"",
                        UseShellExecute = false,
                        CreateNoWindow = false
                    });
                    return;
                }
                catch
                {
                    PlaybackError = "Не установлен VLC или mpv. Установите один из этих плееров.";
                }
            }
        }
        catch (Exception ex)
        {
            PlaybackError = $"Ошибка открытия в VLC/mpv: {ex.Message}";
        }
    }
    
    private void OpenInBrowser()
    {
        try
        {
            var watchUrl = $"http://localhost:5000/api/videos/{Video.Id}/watch";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = watchUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            PlaybackError = $"Не удалось открыть браузер: {ex.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
