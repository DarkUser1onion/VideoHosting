using Avalonia.Controls;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Views;

public partial class PlaylistWindow : Window
{
    public PlaylistWindow()
    {
        InitializeComponent();
    }
    
    public PlaylistWindow(IApiService api)
    {
        InitializeComponent();
        DataContext = new PlaylistViewModel(api);
    }
}
