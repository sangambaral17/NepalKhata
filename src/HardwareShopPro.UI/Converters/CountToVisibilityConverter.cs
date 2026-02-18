using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HardwareShopPro.UI.Converters;

/// <summary>
/// Converts an int count to Visibility: count > 0 → Visible, 0 → Collapsed.
/// Used for search results popup in BillingView.
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
