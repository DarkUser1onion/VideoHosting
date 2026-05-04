using Avalonia.Controls;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Views;

public partial class NotificationsWindow : Window
{
    public NotificationsWindow()
    {
        InitializeComponent();
    }
    
    public NotificationsWindow(IApiService api)
    {
        InitializeComponent();
        DataContext = new NotificationsViewModel(api);
    }
}
