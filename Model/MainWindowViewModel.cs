using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VideoHostingByWhoami.Services;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Model;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    
    // Collections
    public ObservableCollection<VideoItem> Videos { get; } = new();
    public ObservableCollection<Notification> Notifications { get; } = new();
    public ObservableCollection<Playlist> Playlists { get; } = new();
    
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
        set { _selectedCategory = value; OnPropertyChanged(); _ = LoadVideos(); }
    }
    
    // Auth State
    private bool _isAuthenticated;
    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set { _isAuthenticated = value; OnPropertyChanged(); OnPropertyChanged(nameof(UserButton)); }
    }
    
    private User? _currentUser;
    public User? CurrentUser
    {
        get => _currentUser;
        set { _currentUser = value; OnPropertyChanged(); OnPropertyChanged(nameof(UserButton)); }
    }
    
    public string UserButton => IsAuthenticated ? (CurrentUser?.Login ?? "Выход") : "Войти";
    
    public bool IsModerator => IsAuthenticated && (CurrentUser?.Role == "moderator" || CurrentUser?.Role == "admin");
    
    // Auth Panel
    private bool _isAuthVisible;
    public bool IsAuthVisible
    {
        get => _isAuthVisible;
        set { _isAuthVisible = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCatalogVisible)); }
    }
    
    public bool IsCatalogVisible => !IsAuthVisible;
    
    private bool _isLoginMode = true;
    public bool IsLoginMode
    {
        get => _isLoginMode;
        set { _isLoginMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(AuthTitle)); OnPropertyChanged(nameof(AuthButtonText)); OnPropertyChanged(nameof(SwitchAuthText)); }
    }
    
    public string AuthTitle => IsLoginMode ? "Вход" : "Регистрация";
    public string AuthButtonText => IsLoginMode ? "Войти" : "Зарегистрироваться";
    public string SwitchAuthText => IsLoginMode ? "Нет аккаунта? Зарегистрироваться" : "Уже есть аккаунт? Войти";
    
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
    public SimpleCommand<string> SelectCategoryCommand { get; }
    
    public MainWindowViewModel() : this(new ApiService()) { }
    
    public MainWindowViewModel(IApiService api)
    {
        _api = api;
        
        AuthCommand = new SimpleCommand(ToggleAuth);
        DoAuthCommand = new SimpleCommand(async () => await DoAuth());
        SwitchAuthCommand = new SimpleCommand(() => IsLoginMode = !IsLoginMode);
        ShowUploadCommand = new SimpleCommand(ShowUpload);
        ShowPlaylistsCommand = new SimpleCommand(async () => await ShowPlaylists());
        ShowNotificationsCommand = new SimpleCommand(async () => await ShowNotifications());
        ShowModerationCommand = new SimpleCommand(async () => await ShowModeration());
        SelectCategoryCommand = new SimpleCommand<string>(cat => SelectedCategory = cat ?? "");
        
        // Load token from storage
        LoadSavedAuth();
        
        // Initial load
        _ = LoadVideos();
    }
    
    private void LoadSavedAuth()
    {
        // In real app, load from secure storage
        // For now, just check if token exists
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
                Videos.Add(video);
            }
            PageTitle = string.IsNullOrEmpty(SelectedCategory) ? "Все видео" : $"Категория: {SelectedCategory}";
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
            // Logout
            _api.Token = null;
            IsAuthenticated = false;
            CurrentUser = null;
        }
        else
        {
            IsAuthVisible = true;
            AuthError = "";
        }
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
            AuthLogin = "";
            AuthPassword = "";
            await LoadVideos();
        }
        else
        {
            AuthError = result.Message;
        }
    }
    
    public void OpenVideo(VideoItem video)
    {
        var playerWindow = new PlayerWindow(_api, video);
        playerWindow.Show();
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
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
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
            _execute();
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
