using System.Threading.Tasks;
using Avalonia.Controls;
using VideoHostingByWhoami.Model;

namespace VideoHostingByWhoami.Views;

public partial class ConfirmDialogWindow : Window
{
    public ConfirmDialogWindow()
    {
        InitializeComponent();
    }

    public ConfirmDialogWindow(string title, string message)
    {
        InitializeComponent();
        Title = title;
        DataContext = new ConfirmDialogViewModel(message, this);
    }

    public async Task<bool> ShowConfirmDialog(Window owner)
    {
        var result = await base.ShowDialog<bool?>(owner);
        return result == true;
    }
}

public class ConfirmDialogViewModel
{
    private readonly Window _window;
    public string Message { get; }
    public SimpleCommand ConfirmCommand { get; }
    public SimpleCommand CancelCommand { get; }

    public ConfirmDialogViewModel(string message, Window window)
    {
        Message = message;
        _window = window;
        ConfirmCommand = new SimpleCommand(() => _window.Close(true));
        CancelCommand = new SimpleCommand(() => _window.Close(false));
    }
}
