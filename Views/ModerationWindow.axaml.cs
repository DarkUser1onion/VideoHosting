using Avalonia.Controls;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Views;

public partial class ModerationWindow : Window
{
    public ModerationWindow()
    {
        InitializeComponent();
    }
    
    public ModerationWindow(IApiService api)
    {
        InitializeComponent();
        DataContext = new ModerationViewModel(api);
    }
}
