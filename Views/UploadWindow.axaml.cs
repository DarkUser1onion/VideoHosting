using Avalonia.Controls;
using VideoHostingByWhoami.Model;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Views;

public partial class UploadWindow : Window
{
    public UploadWindow()
    {
        InitializeComponent();
    }
    
    public UploadWindow(IApiService api)
    {
        InitializeComponent();
        DataContext = new UploadViewModel(api, this);
    }
}
