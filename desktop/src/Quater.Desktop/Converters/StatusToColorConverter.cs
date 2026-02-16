using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Quater.Desktop.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status.ToLower() switch
            {
                "compliant" => Brushes.ForestGreen,
                "non-compliant" => Brushes.Crimson,
                "warning" => Brushes.Orange,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
