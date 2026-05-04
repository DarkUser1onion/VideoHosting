using Avalonia.Controls;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Views;

public partial class PlayerWindow : Window
{
    public PlayerWindow()
    {
        InitializeComponent();
    }
    
    public PlayerWindow(IApiService api, VideoItem video)
    {
        InitializeComponent();
        DataContext = new PlayerViewModel(api, video);
    }
}
