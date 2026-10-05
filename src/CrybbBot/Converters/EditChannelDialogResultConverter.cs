using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using CrybbBot.Models;
using CrybbBot.ViewModels;

namespace CrybbBot.Converters;

public sealed class EditChannelDialogResultConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool.TryParse(parameter?.ToString() ?? string.Empty, out var yes);
        var newId = values.Length > 0 ? values[0] as string : null;
        var oldId = values.Length > 1 ? values[1] as string : null;
        var newAlias = values.Length > 2 ? values[2] as string : null;
        var oldAlias = values.Length > 3 ? values[3] as string : null;

        ChannelInBundleWithFlagViewModel[]? bundles = null;
        if (values.Length > 4 && values[4] is ObservableCollection<ChannelInBundleWithFlagViewModel> oc)
        {
            bundles = oc.ToArray();
        }

        return new EditChannelDialogResult
        {
            Yes = yes,
            NewId = newId?.Trim(),
            OldId = oldId?.Trim(),
            NewAlias = newAlias?.Trim(),
            OldAlias = oldAlias?.Trim(),
            Bundles = bundles
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
