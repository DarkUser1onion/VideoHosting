#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace VideoHostingByWhoami.Model;

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Если true - полная непрозрачность, иначе полупрозрачный
        if (value is bool isActive && isActive)
            return 1.0;
        return 0.4;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToActiveColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Если true - синий акцентный цвет, иначе обычный белый
        if (value is bool isActive && isActive)
            return new SolidColorBrush(Color.Parse("#3B82F6")); // Акцентный синий
        return new SolidColorBrush(Color.Parse("#FFFFFF")); // Белый
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LikeToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Если значение null или не bool, делаем полупрозрачным
        if (value == null || !(value is bool userLiked))
            return 0.4;

        string expected = parameter as string;

        if (expected != null)
        {
            bool isLike = expected.Equals("like", StringComparison.OrdinalIgnoreCase);
            // Если пользователь поставил лайк и это кнопка лайка - полная непрозрачность
            // Если пользователь поставил дизлайк и это кнопка дизлайка - полная непрозрачность
            if (userLiked == isLike)
                return 1.0;
        }
        
        return 0.4;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LikeColorConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        // values[0] = bool (isActive)
        // values[1] = string (buttonType: "like" или "dislike")
        
        bool isActive = values.Count > 0 && values[0] is bool b && b;
        string buttonType = values.Count > 1 ? values[1]?.ToString() : null;
        
        Console.WriteLine($"LikeColorConverter: isActive={isActive}, buttonType={buttonType}");
        
        // Цвет по умолчанию - серый
        var defaultColor = new SolidColorBrush(Color.Parse("#374151"));
        var activeColor = new SolidColorBrush(Color.Parse("#3B82F6"));
        
        if (isActive)
            return activeColor;
        
        return defaultColor;
    }
}
