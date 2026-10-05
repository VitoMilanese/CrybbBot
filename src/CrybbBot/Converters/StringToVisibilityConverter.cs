using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CrybbBot.Converters;

public sealed class StringToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hasText = value is string s && !string.IsNullOrWhiteSpace(s);

        if (Invert || (parameter?.ToString()?.Equals("Invert", StringComparison.InvariantCultureIgnoreCase) ?? false))
        {
            hasText = !hasText;
        }

        return hasText ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
