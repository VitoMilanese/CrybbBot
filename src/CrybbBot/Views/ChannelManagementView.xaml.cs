using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CrybbBot.ViewModels;
using DataLayer;
using static CrybbBot.ViewModels.EditChannelDialogViewModel;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for ChannelManagementView.xaml
    /// </summary>
    public partial class ChannelManagementView : UserControl
    {
        private ChannelManagementViewModel _vm => (ChannelManagementViewModel)Resources["ViewModel"];

        private Point _dragStartPoint;
        
        private ChannelBundleViewModel? _lastDraggedOverChannelBundle { get; set; }

        public ChannelManagementView()
        {
            InitializeComponent();
        }

        public void Load() => _vm.Load();

        private void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Stop bubbling to ListViewItem so it won't select the item
            e.Handled = true;
        }

        private async void EditBundle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn &&
                btn?.DataContext is ChannelBundleViewModel item &&
                item.CanBeModified)
            {
                var result = await MainWindow.ShowRenameDialog(32, "Переіменування групи каналів", item.Text,
                    new Tuple<string, string>("Стара назва:", "Нова назва:"));
                if (result.Yes && !string.IsNullOrWhiteSpace(result.NewValue?.Trim()))
                {
                    var value = Regex.Replace(result.NewValue.Trim(), @" {2,}", " ");
                    if (_vm.Bundles.Any(p => p.Text.Equals(value, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        await MainWindow.ShowDialog("Помилка", "Така група каналів вже є.");
                    }
                    else
                    {
                        if (item.DbBundle != null)
                        {
                            var tmp = item.Text;
                            try
                            {
                                item.Text = value;
                                item.DbBundle.Alias = value;
                                await DbContext.Data.UpdateBundle(item.DbBundle);
                            }
                            catch
                            {
                                item.Text = tmp;
                                item.DbBundle.Alias = tmp;
                                await MainWindow.ShowDialog(string.Empty, "Під час внесення змін до бази даних виникла помилка");
                            }
                        }
                    }
                }
            }
        }

        private async void DeleteBundle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn &&
                btn?.DataContext is ChannelBundleViewModel item &&
                item.CanBeDeleted)
            {
                var msg = string.Empty;
                if (item.Channels.Any())
                {
                    msg = $"Ви впевнені, що хочете видалити групу каналів \"{item.Text}\"?\r\nКанали, які були у цій групі все ще залишаться у групі \"{Constants.All}\",\r\nа також можуть бути і в інших групах.";
                }
                else
                {
                    msg = $"Ви впевнені, що хочете видалити групу каналів \"{item.Text}\"?\r\nНаразі у цій групі немає каналів.";
                }
                var result = await MainWindow.ShowYesNoDialog(string.Empty, msg);
                if (result)
                {
                    _vm.DeleteBundle(item);
                }
            }
        }

        private async void EditChannel_Click(object sender, RoutedEventArgs e)
        {
            Models.EditChannelDialogResult? result = null;

            if (e is EditChannelDialogRoutedEventArgs vmArgs && vmArgs.ViewModel != null)
            {
                result = await MainWindow.ShowEditChannelDialog(vmArgs.ViewModel);
            }
            else if (sender is Button btn && btn?.DataContext is ChannelViewModel item)
            {
                var editChannelClickInfo = new EditChannelDialogViewModel.EditChannelRoutedEventInfo
                {
                    Clicker = btn,
                    Event = EditChannel_Click
                };
                result = await MainWindow.ShowEditChannelDialog(32, item.Id, item.PureAlias, editChannelClickInfo);
            }

            if (result != null && result.Yes)
            {
                var idChanged = !string.IsNullOrWhiteSpace(result.NewId);
                var id = idChanged ? result.NewId : result.OldId;

                if (!string.IsNullOrWhiteSpace(id))
                {
                    var aliasChanged = !string.IsNullOrWhiteSpace(result.NewAlias);
                    var alias = aliasChanged ? result.NewAlias : result.OldAlias;

                    ChannelInBundleWithFlagViewModel[]? removed = null;
                    ChannelInBundleWithFlagViewModel[]? added = null;

                    if (!string.IsNullOrWhiteSpace(result.OldId))
                    {
                        var changed = result.Bundles?.Where(p => p.Changed);
                        if (changed?.Any() ?? false)
                        {
                            removed = changed.Where(p => !p.IsPresent).ToArray();
                            for (var i = 0; i < removed.Length; ++i)
                            {
                                _vm.DeleteChannel(removed[i].BundleName, result.OldId, result.OldAlias, i == removed.Length - 1);
                            }

                            added = changed.Where(p => p.IsPresent).ToArray();
                            foreach (var bundle in added)
                            {
                                _vm.AddChannel(bundle.BundleName, id, alias, false);
                            }
                        }
                    }

                    if (ChannelBundleViewModel.GlobalCollection?.Any() ?? false)
                    {
                        if (idChanged || aliasChanged)
                        {
                            var managedChannels = new List<string>();
                            foreach (var bundle in ChannelBundleViewModel.GlobalCollection)
                            {
                                if (removed != null && removed.Any(p => p.BundleName.Equals(bundle.Text)))
                                {
                                    continue;
                                }
                                if (added != null && added.Any(p => p.BundleName.Equals(bundle.Text)))
                                {
                                    continue;
                                }

                                var channel = bundle.Channels.FirstOrDefault(p => p.Id.Equals(result.OldId) ||
                                                                             (!string.IsNullOrWhiteSpace(result.OldAlias) &&
                                                                              !string.IsNullOrWhiteSpace(p.PureAlias) &&
                                                                              result.OldAlias.Equals(p.PureAlias)));
                                
                                if (channel != null && (idChanged || aliasChanged))
                                {
                                    if (channel.DbChannel != null)
                                    {
                                        var tmp1 = channel.Id;
                                        var tmp2 = channel.Alias;
                                        try
                                        {
                                            if (idChanged)
                                            {
                                                channel.Id = id;
                                                channel.DbChannel.SlackChannelID = id;
                                            }

                                            if (aliasChanged)
                                            {
                                                channel.Alias = alias;
                                                channel.DbChannel.Alias = alias;
                                            }

                                            if (!managedChannels.Any(p => p.Equals(channel.Id, StringComparison.InvariantCultureIgnoreCase)))
                                            {
                                                await DbContext.Data.UpdateChannel(channel.DbChannel);
                                                managedChannels.Add(channel.Id);
                                            }
                                        }
                                        catch
                                        {
                                            channel.Id = tmp1;
                                            channel.Alias = tmp2;
                                            channel.DbChannel.SlackChannelID = tmp1;
                                            channel.DbChannel.Alias = tmp2;
                                            await MainWindow.ShowDialog(string.Empty, "Під час внесення змін до бази даних виникла помилка");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private async void DeleteChannel_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedBundle == null)
            {
                return;
            }
            if (sender is Button btn &&
                btn?.DataContext is ChannelViewModel item)
            {
                var msg = string.Empty;
                if (item.Id.Equals(item.Alias))
                {
                    msg = $"Ви впевнені, що хочете видалити канал\r\n{item.Id}";
                }
                else
                {
                    msg = $"Ви впевнені, що хочете видалити канал\r\n{item.Id} \"{item.Alias}\"";
                }
                var msg1 = $" з групи \"{_vm.SelectedBundle.Text}\"?";
                msg += msg1;
                if (_vm.Bundles
                    .Where(p => p != _vm.SelectedBundle)
                    .Any(p => p.Channels.Any(q => q.Id.Equals(item.Id) ||
                                                  (!string.IsNullOrWhiteSpace(q.PureAlias) &&
                                                   !string.IsNullOrEmpty(item.PureAlias) &&
                                                   q.PureAlias.Equals(item.PureAlias)))))
                {
                    var msg2 = "\r\nЦей канал буде виделено лише з цієї групи\r\nі все ще залишиться присутнім у інших групах.";
                    msg += msg2;
                }
                var result = await MainWindow.ShowYesNoDialog(string.Empty, msg);
                if (result)
                {
                    _vm.DeleteChannel(item);
                }
            }
        }

        private void Channels_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void Channels_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var pos = e.GetPosition(null);
            var diff = _dragStartPoint - pos;

            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            // Find the dragged Channel item
            var listView = (ListView)sender;
            var container = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            if (container?.DataContext is not ChannelViewModel channel)
                return;

            // Put the channel into drag data (strongly typed)
            var data = new DataObject(typeof(ChannelViewModel), channel);

            DragDrop.DoDragDrop(listView, data, DragDropEffects.Move);
        }

        private void Bundles_PreviewDragOver(object sender, DragEventArgs e)
        {
            // We accept only channels
            if (e.Data.GetDataPresent(typeof(ChannelViewModel)))
            {
                e.Effects = DragDropEffects.Move;

                var pos = e.GetPosition(BundlesList);
                var hit = VisualTreeHelper.HitTest(BundlesList, pos);
                var itemContainer = FindAncestor<ListViewItem>(hit?.VisualHit);

                ChannelBundleViewModel? targetBundle =
                    itemContainer?.DataContext as ChannelBundleViewModel
                    ?? (DataContext as ChannelManagementViewModel)?.SelectedBundle;

                if (targetBundle != null)
                {
                    targetBundle.Highlighted = true;
                    _lastDraggedOverChannelBundle = targetBundle;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void Bundles_PreviewDragLeave(object sender, DragEventArgs e)
        {
            // We accept only channels
            if (e.Data.GetDataPresent(typeof(ChannelViewModel)))
            {
                var pos = e.GetPosition(BundlesList);
                var hit = VisualTreeHelper.HitTest(BundlesList, pos);
                var itemContainer = FindAncestor<ListViewItem>(hit?.VisualHit);

                ChannelBundleViewModel? targetBundle =
                    itemContainer?.DataContext as ChannelBundleViewModel
                    ?? (DataContext as ChannelManagementViewModel)?.SelectedBundle;

                if (targetBundle != null)
                {
                    targetBundle.Highlighted = false;
                }
                else if (_lastDraggedOverChannelBundle != null)
                {
                    _lastDraggedOverChannelBundle.Highlighted = false;
                }
            }

            e.Handled = true;
        }

        private void Bundles_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(ChannelViewModel)))
                return;

            var channel = (ChannelViewModel)e.Data.GetData(typeof(ChannelViewModel))!;

            // Find which Bundle item is under mouse
            var pos = e.GetPosition(BundlesList);
            var hit = VisualTreeHelper.HitTest(BundlesList, pos);
            var itemContainer = FindAncestor<ListViewItem>(hit?.VisualHit);

            // If dropped onto empty space, you can decide behavior:
            // - ignore, or
            // - drop into currently selected bundle, etc.
            ChannelBundleViewModel? targetBundle =
                itemContainer?.DataContext as ChannelBundleViewModel
                ?? (DataContext as ChannelManagementViewModel)?.SelectedBundle;

            if (targetBundle == null)
                return;

            if (targetBundle != _lastDraggedOverChannelBundle)
            {
                targetBundle.Highlighted = false;
            }

            if (_lastDraggedOverChannelBundle != null)
            {
                _lastDraggedOverChannelBundle.Highlighted = false;
                _lastDraggedOverChannelBundle = null;
            }

            // Let VM do the move so collections stay consistent
            _vm.MoveChannelToBundle(channel, targetBundle);

            e.Handled = true;
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T found) return found;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
