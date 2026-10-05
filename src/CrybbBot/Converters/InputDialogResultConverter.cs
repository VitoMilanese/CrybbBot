using System;
using System.Globalization;
using System.Windows.Data;
using CrybbBot.Models;

namespace CrybbBot.Converters;

public sealed class InputDialogResultConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool.TryParse(parameter?.ToString() ?? string.Empty, out var yes);
        var value = values.Length > 0 ? values[0] as string : null;

        return new InputDialogResult
        {
            Yes = yes,
            Value = value
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
