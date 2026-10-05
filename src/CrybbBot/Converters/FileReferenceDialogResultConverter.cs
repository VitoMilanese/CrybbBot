using System;
using System.Globalization;
using System.Windows.Data;
using CrybbBot.Models;

namespace CrybbBot.Converters;

public sealed class FileReferenceDialogResultConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool.TryParse(parameter?.ToString() ?? string.Empty, out var yes);
        var title = values.Length > 0 ? values[0] as string : null;
        var url = values.Length > 1 ? values[1] as string : null;

        return new FileReferenceDialogResult
        {
            Yes = yes,
            Title = title,
            Url = url
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
