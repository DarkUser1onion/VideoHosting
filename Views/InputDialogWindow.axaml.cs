using System.Threading.Tasks;
using Avalonia.Controls;
using VideoHostingByWhoami.Model;

namespace VideoHostingByWhoami.Views;

public partial class InputDialogWindow : Window
{
    public InputDialogWindow()
    {
        InitializeComponent();
    }
    
    public InputDialogWindow(string prompt)
    {
        InitializeComponent();
        DataContext = new InputDialogViewModel(prompt, this);
    }
    
    public Task<string?> ShowDialog()
    {
        var tcs = new TaskCompletionSource<string?>();
        
        Closed += (s, e) =>
        {
            var vm = DataContext as InputDialogViewModel;
            tcs.TrySetResult(vm?.Confirmed == true ? vm.Input : null);
        };
        
        Show();
        
        return tcs.Task;
    }
}

public class InputDialogViewModel
{
    public string Prompt { get; }
    public string Input { get; set; } = string.Empty;
    public bool Confirmed { get; private set; }
    
    public SimpleCommand CancelCommand { get; }
    public SimpleCommand ConfirmCommand { get; }
    
    private readonly Window _window;
    
    public InputDialogViewModel(string prompt, Window window)
    {
        Prompt = prompt;
        _window = window;
        
        CancelCommand = new SimpleCommand(() =>
        {
            Confirmed = false;
            _window.Close();
        });
        
        ConfirmCommand = new SimpleCommand(() =>
        {
            Confirmed = true;
            _window.Close();
        });
    }
}
