using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Views;

public partial class AdminWindow : Window
{
    public AdminWindow()
    {
        InitializeComponent();
    }

    public AdminWindow(IApiService api)
    {
        InitializeComponent();
        DataContext = new AdminWindowViewModel(api);
    }
}

public class AdminWindowViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;

    public AdminWindowViewModel(IApiService api)
    {
        _api = api;
        CreateModeratorCommand = new SimpleCommand(async () => await CreateModerator());
    }

    private string _login = string.Empty;
    public string Login
    {
        get => _login;
        set { _login = value; OnPropertyChanged(); }
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStatus)); }
    }

    private IBrush _statusColor = Brushes.LightGreen;
    public IBrush StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; OnPropertyChanged(); }
    }

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    public SimpleCommand CreateModeratorCommand { get; }

    private async Task CreateModerator()
    {
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            StatusColor = Brushes.IndianRed;
            StatusMessage = "Введите логин и пароль";
            return;
        }

        var result = await _api.CreateModeratorAsync(Login, Password);
        if (result.Success)
        {
            StatusColor = Brushes.LightGreen;
            StatusMessage = "Модератор успешно создан";
            Login = string.Empty;
            Password = string.Empty;
        }
        else
        {
            StatusColor = Brushes.IndianRed;
            StatusMessage = result.Message;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
