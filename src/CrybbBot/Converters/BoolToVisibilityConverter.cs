using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CrybbBot.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool.TryParse(value?.ToString(), out var val);

        if (Invert || (parameter?.ToString()?.Equals("Invert", StringComparison.InvariantCultureIgnoreCase) ?? false))
        {
            val = !val;
        }

        return val ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
