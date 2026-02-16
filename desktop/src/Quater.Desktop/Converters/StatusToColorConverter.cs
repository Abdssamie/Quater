using System;
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
            return status.ToLowerInvariant() switch
            {
                "compliant" or "conforme" => Brushes.ForestGreen,
                "non-compliant" or "non conforme" or "non-conforme" => Brushes.Crimson,
                "warning" or "avertissement" => Brushes.Orange,
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
