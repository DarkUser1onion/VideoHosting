using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using VideoHostingByWhoami.Services;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Model;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private const string LastAuthFileName = "last-auth.json";
    private readonly IApiService _api;
    private readonly string _authStatePath;
    
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
    
    public bool IsCatalogVisible => !IsAuthVisible;


    
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
        
        // Load token from storage
        LoadSavedAuth();
        
        // Initial load
        _ = LoadVideos();
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
            await LoadVideos();
        }
        else
        {
            AuthError = result.Message;
        }
    }
    
    public void OpenVideo(VideoItem video, Avalonia.Controls.Window? ownerWindow = null)
    {
        var playerWindow = new PlayerWindow(_api, video, IsModerator, LoadVideos);
        playerWindow.Show();
    }

    public void Dispose()
    {
        // Nothing to dispose now
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
