using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CrybbBot.Models;
using CrybbBot.ViewModels;

namespace CrybbBot.Views;

public partial class ChannelMultiSelectComboBox : UserControl
{
    private ChannelMultiSelectComboBoxViewModel _vm => (ChannelMultiSelectComboBoxViewModel)DataContext;

    public bool IsDropDownOpen
    {
        get => _vm.IsDropDownOpen;
        set => _vm.IsDropDownOpen = value;
    }

    public ChannelMultiSelectComboBox()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is Window w)
        {
            w.PreviewMouseDown += OnWindowPreviewMouseDown;
            w.Deactivated += OnWindowDeactivated;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is Window w)
        {
            w.PreviewMouseDown -= OnWindowPreviewMouseDown;
            w.Deactivated -= OnWindowDeactivated;
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (DataContext is ChannelMultiSelectComboBoxViewModel vm)
            vm.IsDropDownOpen = false;
    }

    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ChannelMultiSelectComboBoxViewModel vm) return;
        if (!vm.IsDropDownOpen) return;

        // Close when click is outside BOTH the box and popup
        if (!DropDownBox.IsMouseOver && !DropDownPopup.IsMouseOver)
            vm.IsDropDownOpen = false;
    }

    private void DropDownItemClick(object sender, MouseButtonEventArgs e)
    {
        // Ignore direct CheckBox clicks (they toggle via Command)
        if (e.OriginalSource is DependencyObject d)
        {
            var cb = FindAncestor<System.Windows.Controls.CheckBox>(d);
            if (cb != null) return;
        }

        if (DataContext is not ChannelMultiSelectComboBoxViewModel vm) return;

        if (sender is ListBoxItem lbi && lbi.DataContext is ChannelItem item)
        {
            if (vm.ToggleItemCommand.CanExecute(item))
                vm.ToggleItemCommand.Execute(item);

            e.Handled = true;
        }
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T t) return t;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void DropDownOpened(object sender, RoutedEventArgs e)
    {
        SearchBox?.Focus();
    }

    private void OnChannelsMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Always scroll the outer scrollviewer you control
        ChannelsScrollViewer.ScrollToVerticalOffset(ChannelsScrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }
}
