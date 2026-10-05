using System.Windows;
using System.Windows.Controls;
using CrybbBot.ViewModels;

namespace CrybbBot.Views.Dialogs;

public partial class MessagesFilterDialogView : UserControl
{
    public MessagesFilterDialogView()
    {
        InitializeComponent();
    }

    private void BundlesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MessagesFilterDialogViewModel vm)
        {
            vm.UpdateBundles();
        }
    }

    private void BundlesCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MessagesFilterDialogViewModel vm)
        {
            vm.UpdateBundles();
        }
    }

    private void ChannelsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MessagesFilterDialogViewModel vm)
        {
            vm.UpdateChannels();
        }
    }

    private void ChannelsCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MessagesFilterDialogViewModel vm)
        {
            vm.UpdateChannels();
        }
    }
}