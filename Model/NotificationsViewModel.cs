using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Model;

public class NotificationsViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    
    public ObservableCollection<Notification> Notifications { get; } = new();
    
    public NotificationsViewModel(IApiService api)
    {
        _api = api;
        _ = LoadNotifications();
    }
    
    private async Task LoadNotifications()
    {
        var notifications = await _api.GetNotificationsAsync();
        Notifications.Clear();
        foreach (var notification in notifications)
        {
            Notifications.Add(notification);
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

// Converter for notification background
public class NotificationBackgroundConverter : Avalonia.Data.Converters.IValueConverter
{
    public static readonly NotificationBackgroundConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isRead)
        {
            return isRead ? "#1F2937" : "#1E3A5F";
        }
        return "#1F2937";
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
