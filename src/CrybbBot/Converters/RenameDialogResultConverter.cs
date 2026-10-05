using System;
using System.Globalization;
using System.Windows.Data;
using CrybbBot.Models;

namespace CrybbBot.Converters;

public sealed class RenameDialogResultConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool.TryParse(parameter?.ToString() ?? string.Empty, out var yes);
        var newValue = values.Length > 0 ? values[0] as string : null;

        return new RenameDialogResult
        {
            Yes = yes,
            NewValue = newValue
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
