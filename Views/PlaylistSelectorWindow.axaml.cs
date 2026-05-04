using System;
using Avalonia.Controls;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Views;

public partial class PlaylistSelectorWindow : Window
{
    public PlaylistSelectorWindow()
    {
        InitializeComponent();
    }
    
    public PlaylistSelectorWindow(IApiService api, Guid videoId)
    {
        InitializeComponent();
        DataContext = new PlaylistSelectorViewModel(api, videoId, this);
    }
}
