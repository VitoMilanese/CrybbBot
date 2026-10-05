using System;
using System.Globalization;
using System.Windows.Data;
using CrybbBot.Models;

namespace CrybbBot.Converters;

public sealed class LinkDialogResultConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool.TryParse(parameter?.ToString() ?? string.Empty, out var yes);
        var url = values.Length > 0 ? values[0] as string : null;

        return new LinkDialogResult
        {
            Yes = yes,
            Url = url
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
