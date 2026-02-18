using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HardwareShopPro.UI.Converters;

/// <summary>
/// Converts bool to Visibility in inverted fashion: true → Collapsed, false → Visible.
/// Used for "Offline" badge in AI Assistant view.
/// </summary>
public class InvertedBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
            return v != Visibility.Visible;
        return false;
    }
}
