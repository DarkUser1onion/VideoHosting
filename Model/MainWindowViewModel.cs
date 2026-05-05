using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using VideoHostingByWhoami.Services;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Model;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private const string LastAuthFileName = "last-auth.json";
    private readonly IApiService _api;
    private readonly string _authStatePath;
    private MainWindow? _mainWindow;
    
    // Collections
    public ObservableCollection<VideoItem> Videos { get; } = new();
    public ObservableCollection<Notification> Notifications { get; } = new();
    public ObservableCollection<Playlist> Playlists { get; } = new();
    public ObservableCollection<Comment> Comments { get; } = new();
    
    // Search & Filter
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); _ = SearchVideos(); }
    }
    
    private string _selectedCategory = string.Empty;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedCategoryFilter));
            _ = LoadVideos();
        }
    }

    public ObservableCollection<CategoryFilterOption> CategoryFilters { get; } = new()
    {
        new("", "Все"),
        new("education", "Образование"),
        new("entertainment", "Развлечения"),
        new("technology", "Технологии"),
        new("music", "Музыка"),
        new("sport", "Спорт"),
        new("games", "Игры")
    };

    public CategoryFilterOption? SelectedCategoryFilter
    {
        get => CategoryFilters.FirstOrDefault(x => x.Code == SelectedCategory) ?? CategoryFilters.FirstOrDefault();
        set
        {
            if (value == null)
                return;

            SelectedCategory = value.Code;
            OnPropertyChanged();
        }
    }

    private string GetCategoryDisplayName(string categoryCode)
    {
        return CategoryFilters.FirstOrDefault(x => x.Code == categoryCode)?.Name ?? categoryCode;
    }
    
    // Auth State
    private bool _isAuthenticated;
    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set
        {
            _isAuthenticated = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UserButton));
            OnPropertyChanged(nameof(IsModerator));
            OnPropertyChanged(nameof(IsAdmin));
        }
    }
    
    private User? _currentUser;
    public User? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UserButton));
            OnPropertyChanged(nameof(IsModerator));
            OnPropertyChanged(nameof(IsAdmin));
        }
    }
    
    public string UserButton
    {
        get
        {
            if (!IsAuthenticated)
            {
                return string.IsNullOrWhiteSpace(LastLogin) ? "Вход / Регистрация" : $"Войти ({LastLogin})";
            }

            if (!string.IsNullOrWhiteSpace(CurrentUser?.Login))
            {
                return CurrentUser!.Login;
            }

            return "Аккаунт";
        }
    }
    
    public bool IsModerator => IsAuthenticated && (CurrentUser?.Role == "moderator" || CurrentUser?.Role == "admin");
    public bool IsAdmin => IsAuthenticated && CurrentUser?.Role == "admin";
    
    // Auth Panel
    private bool _isAuthVisible;
    public bool IsAuthVisible
    {
        get => _isAuthVisible;
        set { _isAuthVisible = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCatalogVisible)); }
    }
    
    public bool IsCatalogVisible => !IsAuthVisible && !IsPlayerVisible;

    // Player State
    private bool _isPlayerVisible;
    public bool IsPlayerVisible
    {
        get => _isPlayerVisible;
        set 
        { 
            _isPlayerVisible = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(IsCatalogVisible));
        }
    }

    private VideoItem? _currentVideo;
    public VideoItem? CurrentVideo
    {
        get => _currentVideo;
        set
        {
            _currentVideo = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanDeleteVideo));
        }
    }

    private bool _isPlayerReady;
    public bool IsPlayerReady
    {
        get => _isPlayerReady;
        set { _isPlayerReady = value; OnPropertyChanged(); }
    }

    private bool? _userLiked;
    public bool? UserLiked
    {
        get => _userLiked;
        set
        {
            _userLiked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLikeActive));
            OnPropertyChanged(nameof(IsDislikeActive));
        }
    }

    public bool IsLikeActive => UserLiked == true;
    public bool IsDislikeActive => UserLiked == false;

    public bool CanDeleteVideo => CurrentVideo != null && (IsModerator || (IsAuthenticated && CurrentVideo.AuthorId == CurrentUser?.Id));

    private string _newComment = string.Empty;
    public string NewComment
    {
        get => _newComment;
        set { _newComment = value; OnPropertyChanged(); }
    }

    private bool _isModeratorOrAdmin = false;
    public bool CanDeleteComments => _isModeratorOrAdmin;

    private string _playerError = string.Empty;
    public string PlayerError
    {
        get => _playerError;
        set { _playerError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPlayerError)); }
    }

    public bool HasPlayerError => !string.IsNullOrWhiteSpace(PlayerError);

    private bool _isLoginMode = true;
    public bool IsLoginMode
    {
        get => _isLoginMode;
        set
        {
            _isLoginMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AuthTitle));
            OnPropertyChanged(nameof(AuthButtonText));
            OnPropertyChanged(nameof(SwitchAuthText));
            OnPropertyChanged(nameof(IsRegisterMode));
        }
    }
    
    public string AuthTitle => IsLoginMode ? "Вход" : "Регистрация";
    public string AuthButtonText => IsLoginMode ? "Войти" : "Зарегистрироваться";
    public string SwitchAuthText => IsLoginMode ? "Нет аккаунта? Зарегистрироваться" : "Уже есть аккаунт? Войти";
    public bool IsRegisterMode => !IsLoginMode;
    
    private string _authLogin = string.Empty;
    public string AuthLogin
    {
        get => _authLogin;
        set { _authLogin = value; OnPropertyChanged(); }
    }
    
    private string _authPassword = string.Empty;
    public string AuthPassword
    {
        get => _authPassword;
        set { _authPassword = value; OnPropertyChanged(); }
    }

    private string _lastLogin = string.Empty;
    public string LastLogin
    {
        get => _lastLogin;
        set { _lastLogin = value; OnPropertyChanged(); OnPropertyChanged(nameof(UserButton)); }
    }
    
    private string _authError = string.Empty;
    public string AuthError
    {
        get => _authError;
        set { _authError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasAuthError)); }
    }
    
    public bool HasAuthError => !string.IsNullOrEmpty(AuthError);
    
    // Page state
    private string _pageTitle = "Каталог видео";
    public string PageTitle
    {
        get => _pageTitle;
        set { _pageTitle = value; OnPropertyChanged(); }
    }
    
    // Commands
    public SimpleCommand AuthCommand { get; }
    public SimpleCommand DoAuthCommand { get; }
    public SimpleCommand SwitchAuthCommand { get; }
    public SimpleCommand ShowUploadCommand { get; }
    public SimpleCommand ShowPlaylistsCommand { get; }
    public SimpleCommand ShowNotificationsCommand { get; }
    public SimpleCommand ShowModerationCommand { get; }
    public SimpleCommand ShowAdminPanelCommand { get; }
    public SimpleCommand<string> SelectCategoryCommand { get; }
    public SimpleCommand ClosePlayerCommand { get; }
    public SimpleCommand LikeCommand { get; }
    public SimpleCommand DislikeCommand { get; }
    public SimpleCommand SendCommentCommand { get; }
    public SimpleCommand AddToPlaylistCommand { get; }
    public SimpleCommand OpenInMpvCommand { get; }
    public SimpleCommand OpenInBrowserCommand { get; }
    public SimpleCommand DeleteVideoCommand { get; }
    public SimpleCommand<Guid> DeleteCommentCommand { get; }
    
    public MainWindowViewModel() : this(new ApiService()) { }
    
    public MainWindowViewModel(IApiService api)
    {
        _api = api;
        _authStatePath = BuildAuthStatePath();
        
        AuthCommand = new SimpleCommand(ToggleAuth);
        DoAuthCommand = new SimpleCommand(async () => await DoAuth());
        SwitchAuthCommand = new SimpleCommand(() =>
        {
            IsLoginMode = !IsLoginMode;
        });
        ShowUploadCommand = new SimpleCommand(ShowUpload);
        ShowPlaylistsCommand = new SimpleCommand(async () => await ShowPlaylists());
        ShowNotificationsCommand = new SimpleCommand(async () => await ShowNotifications());
        ShowModerationCommand = new SimpleCommand(async () => await ShowModeration());
        ShowAdminPanelCommand = new SimpleCommand(ShowAdminPanel);
        SelectCategoryCommand = new SimpleCommand<string>(cat => SelectedCategory = cat ?? "");
        ClosePlayerCommand = new SimpleCommand(ClosePlayer);
        LikeCommand = new SimpleCommand(async () => await SetLike(true));
        DislikeCommand = new SimpleCommand(async () => await SetLike(false));
        SendCommentCommand = new SimpleCommand(async () => await SendComment());
        AddToPlaylistCommand = new SimpleCommand(ShowPlaylistSelector);
        DeleteVideoCommand = new SimpleCommand(async () => await DeleteVideo());
        DeleteCommentCommand = new SimpleCommand<Guid>(async id => await DeleteComment(id));
        OpenInMpvCommand = new SimpleCommand(OpenInMpv);
        OpenInBrowserCommand = new SimpleCommand(OpenInBrowser);
        
        // Check user role
        if (_api.IsAuthenticated && _api.CurrentUser != null)
        {
            var role = _api.CurrentUser.Role;
            _isModeratorOrAdmin = role == "moderator" || role == "admin";
        }
        
        // Load token from storage
        LoadSavedAuth();
        
        // Initial load
        _ = LoadVideos();
    }

    public void SetMainWindow(MainWindow window)
    {
        _mainWindow = window;
    }
    
    private void LoadSavedAuth()
    {
        try
        {
            if (!File.Exists(_authStatePath))
                return;

            var json = File.ReadAllText(_authStatePath);
            var state = JsonSerializer.Deserialize<AuthState>(json);
            if (!string.IsNullOrWhiteSpace(state?.LastLogin))
            {
                LastLogin = state.LastLogin;
                AuthLogin = state.LastLogin;
            }
        }
        catch
        {
            // Ignore persistence errors to keep app usable.
        }
    }

    private void SaveAuthState()
    {
        try
        {
            var directory = Path.GetDirectoryName(_authStatePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var state = new AuthState(LastLogin);
            var json = JsonSerializer.Serialize(state);
            File.WriteAllText(_authStatePath, json);
        }
        catch
        {
            // Ignore persistence errors to keep app usable.
        }
    }

    private static string BuildAuthStatePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "VideoHostingByWhoami", LastAuthFileName);
    }
    
    public async Task LoadVideos()
    {
        try
        {
            var videos = await _api.GetVideosAsync(SearchText, SelectedCategory);
            Videos.Clear();
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
                    // Do not break catalog rendering if preview decode fails.
                    video.PreviewImage = null;
                }
                Videos.Add(video);
            }
            PageTitle = string.IsNullOrEmpty(SelectedCategory)
                ? "Все видео"
                : $"Категория: {GetCategoryDisplayName(SelectedCategory)}";
        }
        catch
        {
            // Handle error
        }
    }
    
    private async Task SearchVideos()
    {
        await LoadVideos();
    }
    
    private void ToggleAuth()
    {
        if (IsAuthenticated)
        {
            Logout();
        }
        else
        {
            ShowAuthPanel();
        }
    }

    public void ShowAuthPanel()
    {
        IsLoginMode = true;
        IsAuthVisible = true;
        AuthError = "";
        if (!string.IsNullOrWhiteSpace(LastLogin))
        {
            AuthLogin = LastLogin;
        }
    }

    public void Logout()
    {
        _api.Token = null;
        IsAuthenticated = false;
        CurrentUser = null;
        AuthPassword = "";
        _isModeratorOrAdmin = false;
    }
    
    private async Task DoAuth()
    {
        if (string.IsNullOrWhiteSpace(AuthLogin) || string.IsNullOrWhiteSpace(AuthPassword))
        {
            AuthError = "Введите логин и пароль";
            return;
        }
        
        AuthResult result;
        if (IsLoginMode)
        {
            result = await _api.LoginAsync(AuthLogin, AuthPassword);
        }
        else
        {
            result = await _api.RegisterAsync(AuthLogin, AuthPassword);
        }
        
        if (result.Success)
        {
            IsAuthenticated = true;
            CurrentUser = result.User;
            IsAuthVisible = false;
            AuthError = "";
            LastLogin = AuthLogin;
            SaveAuthState();
            AuthLogin = "";
            AuthPassword = "";
            
            // Update moderator status
            if (_api.CurrentUser != null)
            {
                var role = _api.CurrentUser.Role;
                _isModeratorOrAdmin = role == "moderator" || role == "admin";
                OnPropertyChanged(nameof(CanDeleteComments));
            }
            
            await LoadVideos();
        }
        else
        {
            AuthError = result.Message;
        }
    }

    public void OpenVideo(VideoItem video, MainWindow? mainWindow = null)
    {
        _mainWindow = mainWindow ?? _mainWindow;
        CurrentVideo = video;
        IsPlayerVisible = true;
        IsAuthVisible = false;
        PlayerError = "";
        
        // Reset mpv process
        _mpvProcess = null;
        
        // Load comments and like status
        _ = LoadPlayerData();
    }

    private async Task LoadPlayerData()
    {
        if (CurrentVideo == null) return;

        // Load updated video info
        var updatedVideo = await _api.GetVideoAsync(CurrentVideo.Id);
        if (updatedVideo != null)
        {
            CurrentVideo.Likes = updatedVideo.Likes;
            CurrentVideo.Dislikes = updatedVideo.Dislikes;
            CurrentVideo.Views = updatedVideo.Views;
            OnPropertyChanged(nameof(CurrentVideo));
        }

        // Load comments and like status in parallel
        await Task.WhenAll(LoadComments(), LoadLikeStatus());
    }

    private async Task LoadComments()
    {
        if (CurrentVideo == null) return;
        
        var comments = await _api.GetCommentsAsync(CurrentVideo.Id);
        Comments.Clear();
        foreach (var comment in comments)
            Comments.Add(comment);
    }

    private async Task LoadLikeStatus()
    {
        if (CurrentVideo == null) return;
        
        if (_api.IsAuthenticated)
            UserLiked = await _api.GetLikeStatusAsync(CurrentVideo.Id);
    }

    public void ClosePlayer()
    {
        IsPlayerVisible = false;
        CurrentVideo = null;
        Comments.Clear();
        UserLiked = null;
        NewComment = "";
        PlayerError = "";
        
        // Refresh video list
        _ = LoadVideos();
    }

    private async Task SetLike(bool isLike)
    {
        if (CurrentVideo == null) return;
        
        PlayerError = "";
        if (!_api.IsAuthenticated)
        {
            PlayerError = "Нужно войти в аккаунт, чтобы ставить лайки";
            return;
        }

        var success = await _api.SetLikeAsync(CurrentVideo.Id, isLike);
        if (success)
        {
            UserLiked = await _api.GetLikeStatusAsync(CurrentVideo.Id);
            var updated = await _api.GetVideoAsync(CurrentVideo.Id);
            if (updated != null)
            {
                CurrentVideo.Likes = updated.Likes;
                CurrentVideo.Dislikes = updated.Dislikes;
                CurrentVideo.Views = updated.Views;
                OnPropertyChanged(nameof(CurrentVideo));
            }
        }
        else
        {
            PlayerError = "Не удалось изменить лайк";
        }
    }

    private async Task SendComment()
    {
        if (CurrentVideo == null || !_api.IsAuthenticated || string.IsNullOrWhiteSpace(NewComment)) return;

        var success = await _api.AddCommentAsync(CurrentVideo.Id, NewComment);
        if (success)
        {
            NewComment = "";
            await LoadComments();
        }
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

    private void ShowPlaylistSelector()
    {
        if (CurrentVideo == null) return;
        
        var selector = new PlaylistSelectorWindow(_api, CurrentVideo.Id);
        selector.Show();
    }

    private async Task DeleteVideo()
    {
        if (CurrentVideo == null || !CanDeleteVideo || _mainWindow == null) return;

        var confirmWindow = new DeleteConfirmWindow();
        var confirmed = await confirmWindow.ShowConfirmDialog(_mainWindow);
        if (!confirmed) return;

        var deleted = await _api.DeleteVideoAsync(CurrentVideo.Id);
        if (!deleted) return;

        ClosePlayer();
    }

    private Process? _mpvProcess;
    
    private bool IsMpvRunning => _mpvProcess != null && !_mpvProcess.HasExited;
    
    private void OpenInMpv()
    {
        if (CurrentVideo == null) return;
        
        // Если mpv уже запущен - ничего не делаем
        if (IsMpvRunning)
        {
            return;
        }
        
        var streamUrl = $"http://localhost:5000/api/videos/{CurrentVideo.Id}/stream";
        
        try
        {
            // Записываем просмотр при открытии MPV
            _ = RecordViewAsync();
            
            _mpvProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "mpv",
                    Arguments = $"--title=\"{CurrentVideo.Title}\" \"{streamUrl}\"",
                    UseShellExecute = false,
                    CreateNoWindow = false
                },
                EnableRaisingEvents = true
            };
            
            _mpvProcess.Exited += (s, e) =>
            {
                // Не обновляем окно при закрытии mpv
                _mpvProcess = null;
            };
            
            _mpvProcess.Start();
        }
        catch
        {
            PlayerError = "Не удалось запустить mpv. Убедитесь, что mpv установлен.";
        }
    }
    
    private async Task RecordViewAsync()
    {
        if (CurrentVideo == null) return;
        
        try
        {
            using var client = new System.Net.Http.HttpClient();
            await client.PostAsync($"http://localhost:5000/api/videos/{CurrentVideo.Id}/view", null);
        }
        catch
        {
            // Игнорируем ошибки записи просмотра
        }
    }
    
    private void OpenInBrowser()
    {
        if (CurrentVideo == null) return;
        
        var watchUrl = $"http://localhost:5000/api/videos/{CurrentVideo.Id}/watch";
        
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = watchUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            PlayerError = "Не удалось открыть браузер.";
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
    
    private void ShowUpload()
    {
        var uploadWindow = new UploadWindow(_api);
        uploadWindow.Show();
        uploadWindow.Closed += async (s, e) => await LoadVideos();
    }
    
    private async Task ShowPlaylists()
    {
        var playlistWindow = new PlaylistWindow(_api);
        playlistWindow.Show();
    }
    
    private async Task ShowNotifications()
    {
        var notificationsWindow = new NotificationsWindow(_api);
        notificationsWindow.Show();
    }
    
    private async Task ShowModeration()
    {
        var moderationWindow = new ModerationWindow(_api);
        moderationWindow.Show();
        moderationWindow.Closed += async (s, e) => await LoadVideos();
    }

    private void ShowAdminPanel()
    {
        var adminWindow = new AdminWindow(_api);
        adminWindow.Show();
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public record AuthState(string LastLogin);
public record CategoryFilterOption(string Code, string Name)
{
    public override string ToString() => Name;
}

public class SimpleCommand : System.Windows.Input.ICommand
{
    private readonly Action? _execute;
    private readonly Action<object?>? _executeWithParam;
    private readonly Func<bool>? _canExecute;
    
    public SimpleCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    
    public SimpleCommand(Action<object?> execute, Func<bool>? canExecute = null)
    {
        _executeWithParam = execute;
        _canExecute = canExecute;
    }
    
    public event EventHandler? CanExecuteChanged;
    
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    
    public void Execute(object? parameter)
    {
        if (_executeWithParam != null)
            _executeWithParam(parameter);
        else
            _execute?.Invoke();
    }
    
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class SimpleCommand<T> : System.Windows.Input.ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<bool>? _canExecute;
    
    public SimpleCommand(Action<T?> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    
    public event EventHandler? CanExecuteChanged;
    
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    
    public void Execute(object? parameter) => _execute(parameter is T t ? t : default);
    
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
