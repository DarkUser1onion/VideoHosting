using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void OnVideoClick(object sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is VideoItem video)
        {
            var vm = DataContext as MainWindowViewModel;
            vm?.OpenVideo(video);
        }
    }
}
