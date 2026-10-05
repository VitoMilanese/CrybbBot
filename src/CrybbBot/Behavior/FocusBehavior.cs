using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CrybbBot.Behavior
{
    public static class FocusBehavior
    {
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsFocused",
                typeof(bool),
                typeof(FocusBehavior),
                new FrameworkPropertyMetadata(false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsFocusedChanged));

        public static bool GetIsFocused(DependencyObject obj)
            => (bool)obj.GetValue(IsFocusedProperty);

        public static void SetIsFocused(DependencyObject obj, bool value)
            => obj.SetValue(IsFocusedProperty, value);

        private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not bool b || !b) return;
            if (d is not FrameworkElement fe) return;

            // If not loaded yet, wait
            if (!fe.IsLoaded)
            {
                RoutedEventHandler? loaded = null;
                loaded = (_, __) =>
                {
                    fe.Loaded -= loaded;
                    FocusAfterDialogHostSettles(fe);
                };
                fe.Loaded += loaded;
                return;
            }

            FocusAfterDialogHostSettles(fe);
        }

        private static void FocusAfterDialogHostSettles(FrameworkElement fe)
        {
            // DialogHost may still be animating and will steal focus;
            // schedule late (Render) and then again at ApplicationIdle.
            fe.Dispatcher.BeginInvoke(new Action(() => FocusNow(fe)), DispatcherPriority.Render);
            fe.Dispatcher.BeginInvoke(new Action(() => FocusNow(fe)), DispatcherPriority.ApplicationIdle);
        }

        private static void FocusNow(FrameworkElement fe)
        {
            if (!fe.IsVisible || !fe.IsEnabled) return;

            // Make sure focus is allowed
            fe.Focusable = true;

            if (fe is TextBox tb)
            {
                tb.Focusable = true;
                tb.IsTabStop = true;

                // strongest focus method
                Keyboard.Focus(tb);

                // select after focus
                tb.SelectAll();

                // reset flag
                SetIsFocused(tb, false);
                return;
            }

            // Generic control
            Keyboard.Focus(fe);
            SetIsFocused(fe, false);
        }
    }
}
