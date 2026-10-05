using System;
using System.Globalization;
using System.Windows.Data;

namespace CrybbBot.Converters;

public sealed class SubtractConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double original)
            return 0;

        if (parameter == null)
            return original;

        if (!double.TryParse(parameter.ToString(), out double subtract))
            return original;

        var result = original - subtract;

        // Prevent negative width
        return result < 0 ? 0 : result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
