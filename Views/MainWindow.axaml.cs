using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext ??= new MainWindowViewModel();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Dispose();
        }

        base.OnClosed(e);
    }
    
    private void OnVideoClick(object sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is VideoItem video)
        {
            var vm = DataContext as MainWindowViewModel;
            vm?.OpenVideo(video, this);
        }
    }

    private void OnLogoClick(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ClosePlayer();
        }
    }

    private async void OnAuthButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (!vm.IsAuthenticated)
        {
            vm.ShowAuthPanel();
            return;
        }

        var confirmWindow = new ConfirmDialogWindow(
            "Подтверждение выхода",
            "Вы уверены, что хотите выйти из аккаунта?"
        );
        var shouldLogout = await confirmWindow.ShowConfirmDialog(this);
        if (shouldLogout)
        {
            vm.Logout();
        }
    }

    private void OnAuthInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.DoAuthCommand.CanExecute(null))
        {
            vm.DoAuthCommand.Execute(null);
            e.Handled = true;
        }
    }
}
