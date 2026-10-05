using System;
using System.Globalization;
using System.Windows.Data;

namespace CrybbBot.Converters;

public sealed class ObjectToBoolConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var condition = value != null;

        if (Invert || (parameter?.ToString()?.Equals("Invert", StringComparison.InvariantCultureIgnoreCase) ?? false))
        {
            condition = !condition;
        }

        return condition;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
