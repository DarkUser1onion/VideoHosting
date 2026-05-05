using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace VideoHostingByWhoami.Views;

public partial class DeleteConfirmWindow : Window
{
    private int _secondsLeft = 3;
    private bool _confirmed;
    private DispatcherTimer? _timer;

    public DeleteConfirmWindow()
    {
        InitializeComponent();
        UpdateTimerText();
        StartCountdown();
    }

    public async Task<bool> ShowConfirmDialog(Window owner)
    {
        await ShowDialog(owner);
        return _confirmed;
    }

    private void StartCountdown()
    {
        _timer = new DispatcherTimer { Interval = System.TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            _secondsLeft--;
            if (_secondsLeft <= 0)
            {
                _timer?.Stop();
                ConfirmButton.IsEnabled = true;
                ConfirmButton.Content = "Подтвердить";
                TimerText.Text = "Теперь можно подтвердить удаление.";
                return;
            }

            UpdateTimerText();
        };
        _timer.Start();
    }

    private void UpdateTimerText()
    {
        TimerText.Text = $"Кнопка подтверждения станет активной через {_secondsLeft} сек.";
        ConfirmButton.Content = $"Подтвердить ({_secondsLeft})";
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        _confirmed = false;
        Close();
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (!ConfirmButton.IsEnabled)
            return;

        _confirmed = true;
        Close();
    }
}
