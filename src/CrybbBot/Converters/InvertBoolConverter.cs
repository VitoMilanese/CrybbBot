using System;
using System.Globalization;
using System.Windows.Data;

namespace CrybbBot.Converters;

public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : Binding.DoNothing;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public sealed class InvertBoolConverter2 : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool.TryParse(value?.ToString(), out var val);

        if (Invert || (parameter?.ToString()?.Equals("Invert", StringComparison.InvariantCultureIgnoreCase) ?? false))
        {
            val = !val;
        }

        return val;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
