using System;
using System.Globalization;
using System.Windows.Data;
using CrybbBot.Models;

namespace CrybbBot.Converters;

public sealed class AddChannelDialogResultConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool.TryParse(parameter?.ToString() ?? string.Empty, out var yes);
        var id = values.Length > 0 ? values[0] as string : null;
        var alias = values.Length > 0 ? values[1] as string : null;

        return new AddChannelDialogResult
        {
            Yes = yes,
            ChannelId = id ?? string.Empty,
            Alias = alias
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
