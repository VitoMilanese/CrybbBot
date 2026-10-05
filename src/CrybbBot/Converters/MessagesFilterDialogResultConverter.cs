using System;
using System.Globalization;
using System.Windows.Data;
using CrybbBot.Models;
using DataLayer.Models;

namespace CrybbBot.Converters;

public sealed class MessagesFilterDialogResultConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool.TryParse(parameter?.ToString() ?? string.Empty, out var yes);
        var value = values.Length > 0 ? values[0] as MessagesFilter : null;

        return new MessagesFilterDialogResult
        {
            Yes = yes,
            Filter = value
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
