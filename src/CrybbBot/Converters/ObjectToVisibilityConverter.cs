using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CrybbBot.Converters;

public sealed class ObjectToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var condition = value != null;

        if (Invert || (parameter?.ToString()?.Equals("Invert", StringComparison.InvariantCultureIgnoreCase) ?? false))
        {
            condition = !condition;
        }

        return condition ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
