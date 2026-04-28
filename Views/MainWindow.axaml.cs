using Avalonia.Controls;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Views;

namespace VideoHostingByWhoami.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        public void OnVideoDoubleTapped(object sender, Avalonia.Input.TappedEventArgs e)
        {
            if (sender is Border border && border.DataContext is VideoItem video)
            {
                (DataContext as MainWindowViewModel)?.OpenVideo(video);
            }
        }
    }
    
}
