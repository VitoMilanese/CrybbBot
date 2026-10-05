using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DataLayer;
using DataLayer.Exceptions;
using DataLayer.Models;
using static CrybbBot.ViewModels.EditChannelDialogViewModel;

namespace CrybbBot.ViewModels
{
    public class ChannelManagementViewModel : ModelBase
    {
        private static ObservableCollection<ChannelBundleViewModel> _bundles { get; } = new();
        public ObservableCollection<ChannelBundleViewModel> Bundles => _bundles;


        private ChannelBundleViewModel? _selectedBundle;
        public ChannelBundleViewModel? SelectedBundle
        {
            get => _selectedBundle;
            set
            {
                _selectedBundle = value;
                RaisePropertyChanged();

                Channels.Clear();

                try
                {
                    LoadChannelsFromDb(_selectedBundle).ConfigureAwait(false);
                }
                catch
                {
                }

                //if (_selectedBundle != null)
                //{
                //    foreach (var channel in _selectedBundle.Channels)
                //    {
                //        Channels.Add(channel);
                //    }
                //}
            }
        }

        private static ObservableCollection<ChannelViewModel> _channels { get; } = new();
        public ObservableCollection<ChannelViewModel> Channels => _channels;

        private ChannelViewModel? _selectedChannel;
        public ChannelViewModel? SelectedChannel
        {
            get => _selectedChannel;
            set
            {
                _selectedChannel = value;
                RaisePropertyChanged();
            }
        }

        public ICommand AddBundleCommand { get; }
        public ICommand AddChannelCommand { get; }

        public ChannelManagementViewModel()
        {
            Channels.CollectionChanged += Channels_CollectionChanged;

            ChannelBundleViewModel.GlobalCollection = Bundles;

            AddBundleCommand = new RelayCommand(AddBundle);
            AddChannelCommand = new RelayCommand(AddChannel);
        }

        public async void Load()
        {
            Bundles.Clear();
            Bundles.Add(new ChannelBundleViewModel { Text = Constants.All, CanBeDeleted = false, CanBeModified = false });

            try
            {
                await LoadBundlesFromDb();
            }
            catch (Exception ex)
            {
                await MainWindow.ShowDialog("Помилка при занесенні до бази даних", ex.Message);
            }

            if (Bundles.Any())
            {
                SelectedBundle = Bundles.First();
            }
        }

        private void Channels_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
            }
        }

        private async Task LoadBundlesFromDb()
        {
            //var bundles = await SqlServerBootstrap.GetAllBundles();
            var bundles = await DbContext.Data.GetAllBundles();
            var tmpBundles = new List<ChannelBundleViewModel>();
            foreach (var bundle in bundles)
            {
                tmpBundles.Add(new ChannelBundleViewModel { DbBundle = bundle, Text = bundle.Alias ?? "Без назви" });
            }
            if (tmpBundles.Any())
            {
                foreach (var bundle in tmpBundles.OrderBy(p => p.Text))
                {
                    Bundles.Add(bundle);

                    bundle.Channels.Clear();
                    var channels = await GetChannelsFromDb(bundle);
                    if (channels?.Any() ?? false)
                    {
                        foreach (var channel in channels)
                        {
                            bundle.Channels.Add(channel);
                        }
                    }
                }
            }
            var all = Bundles.FirstOrDefault(p => p.DbBundle == null);
            if (all != null)
            {
                all.Channels.Clear();
                var channels = await GetChannelsFromDb(all);
                if (channels?.Any() ?? false)
                {
                    foreach (var channel in channels)
                    {
                        all.Channels.Add(channel);
                    }
                }
            }
        }

        private async Task LoadChannelsFromDb(ChannelBundleViewModel? selectedBundle)
        {
            if (SelectedBundle == null)
            {
                return;
            }

            var isAll = SelectedBundle.DbBundle == null;
            //var bundle = isAll ? null : await SqlServerBootstrap.GetBundle(SelectedBundle.DbBundle!.ID);
            var bundle = isAll ? null : await DbContext.Data.GetBundle(SelectedBundle.DbBundle!.ID);
            if (bundle == null && !isAll)
            {
                return;
            }

            Channels.Clear();
            SelectedBundle.Channels.Clear();
            var tmpChannels = new List<ChannelViewModel>();
            if (bundle?.Channels?.Any() ?? false)
            {
                //bundle.Channels.Clear();
                foreach (var channel in bundle.Channels)
                {
                    tmpChannels.Add(new ChannelViewModel { DbChannel = channel, Id = channel.SlackChannelID, Alias = channel.Alias, CanBeDeleted = !isAll });
                }
            }

            if (isAll)
            {
                //var channels = await SqlServerBootstrap.GetAllChannels(bundle?.ID);
                var channels = await DbContext.Data.GetAllChannels(bundle?.ID);
                foreach (var channel in channels)
                {
                    tmpChannels.Add(new ChannelViewModel { DbChannel = channel, Id = channel.SlackChannelID, Alias = channel.Alias, CanBeDeleted = !(channel.BundleChannelMappings?.Any() ?? false) });
                }
            }

            if (tmpChannels.Any())
            {
                foreach (var channel in tmpChannels.OrderBy(p => p.Alias))
                {
                    SelectedBundle.Channels.Add(channel);
                    Channels.Add(channel);
                }
            }
        }

        private async Task<List<ChannelViewModel>?> GetChannelsFromDb(ChannelBundleViewModel selectedBundle)
        {
            if (selectedBundle.DbBundle == null)
            {
                //var allChannels = await SqlServerBootstrap.GetAllChannels(null);
                var allChannels = await DbContext.Data.GetAllChannels(null);
                return allChannels.Select(p => new ChannelViewModel
                {
                    DbChannel = p,
                    Id = p.SlackChannelID,
                    Alias = p.Alias,
                    CanBeDeleted = false
                }).ToList();
            }

            if (selectedBundle.DbBundle == null)
            {
                return null;
            }

            //var bundle = await SqlServerBootstrap.GetBundle(selectedBundle.DbBundle!.ID);
            var bundle = await DbContext.Data.GetBundle(selectedBundle.DbBundle!.ID);
            if (bundle == null)
            {
                return null;
            }

            var tmpChannels = new List<ChannelViewModel>();
            if (bundle?.Channels?.Any() ?? false)
            {
                foreach (var channel in bundle.Channels)
                {
                    tmpChannels.Add(new ChannelViewModel { DbChannel = channel, Id = channel.SlackChannelID, Alias = channel.Alias, CanBeDeleted = true });
                }
            }

            return tmpChannels;
        }

        private async void AddBundle()
        {
            var result = await MainWindow.ShowInputDialog(32, null, "Вкажіть назву нової групи каналів:");
            if (result.Yes && !string.IsNullOrWhiteSpace(result.Value?.Trim()))
            {
                var value = Regex.Replace(result.Value.Trim(), @" {2,}", " ");
                if (Bundles.Any(p => p.Text.Equals(value, StringComparison.InvariantCultureIgnoreCase)))
                {
                    await MainWindow.ShowDialog("Помилка", "Така група каналів вже є.");
                }
                else
                {
                    try
                    {
                        var bundle = new Bundle
                        {
                            Alias = value
                        };
                        //await SqlServerBootstrap.AddBundle(bundle);
                        await DbContext.Data.AddBundle(bundle);
                        Bundles.Add(new ChannelBundleViewModel { Text = value, DbBundle = bundle });
                    }
                    catch (BundleAlreadyExistsException)
                    {
                        await MainWindow.ShowDialog("Помилка", "Група каналів з такою назвою або ID вже є у базі даних.");
                    }
                    catch (Exception ex)
                    {
                        await MainWindow.ShowDialog("Помилка", ex.Message);
                    }
                }
            }
        }

        public async void DeleteBundle(ChannelBundleViewModel item)
        {
            if (Bundles.Contains(item))
            {
                try
                {
                    if (item.DbBundle != null)
                    {
                        //await SqlServerBootstrap.DeleteBundle(item.DbBundle.ID);
                        await DbContext.Data.DeleteBundle(item.DbBundle.ID);
                    }
                    Bundles.Remove(item);

                    foreach (var channel in item.Channels)
                    {
                        ChannelBundleViewModel.RefreshChannelCanBeDeletedFromAll(channel);
                    }
                }
                catch (Exception ex)
                {
                    await MainWindow.ShowDialog("Помилка", ex.Message);
                }
            }
        }

        private async void AddChannel()
        {
            //await SqlServerBootstrap.GetAllMessages(DateTime.Today);
            //await DbContext.Data.GetAllMessages(DateTime.Today, true);

            var result = await MainWindow.ShowAddChannelDialog();
            if (result.Yes && !string.IsNullOrWhiteSpace(result.ChannelId?.Trim()))
            {
                var id = Regex.Replace(result.ChannelId.Trim(), @" {2,}", " ");
                var alias = string.IsNullOrWhiteSpace(result.Alias)
                    ? null
                    : Regex.Replace(result.Alias.Trim(), @" {2,}", " ");
                if (id.Contains(' '))
                {
                    await MainWindow.ShowDialog("Помилка", "ID каналу не повинно містити пробілів.");
                }
                else if (Channels.Any(p => p.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase)))
                {
                    await MainWindow.ShowDialog("Помилка", "Канал з таким ID вже є у цій групі.");
                }
                else
                {
                    foreach (var bundle in Bundles)
                    {
                        var found = bundle.Channels.FirstOrDefault(p => p.Id.Equals(id) ||
                                                                      (!string.IsNullOrWhiteSpace(alias) &&
                                                                       !string.IsNullOrWhiteSpace(p.PureAlias) &&
                                                                       alias.Equals(p.PureAlias, StringComparison.InvariantCultureIgnoreCase)));
                        if (found != null)
                        {
                            await MainWindow.ShowDialog("Помилка", $"Канал з такою назвою або ID вже існує.");
                            EditChannel_Click(found, new RoutedEventArgs());
                            return;
                        }
                    }

                    var dbChannel = new Channel
                    {
                        SlackChannelID = id,
                        Alias = alias,
                        CanBeDeleted = true,
                        BundleChannelMappings = new List<BundleChannelMapping>()
                    };

                    try
                    {
                        //await SqlServerBootstrap.AddChannel(dbChannel);
                        await DbContext.Data.AddChannel(dbChannel);
                        await LoadChannelsFromDb(SelectedBundle);
                    }
                    catch (ChannelAlreadyExistsException)
                    {
                        await MainWindow.ShowDialog("Помилка", "Канал з такою назвою або ID вже існує.");
                    }
                    catch (Exception ex)
                    {
                        await MainWindow.ShowDialog("Помилка", ex.Message);
                        return;
                    }

                    if (SelectedBundle?.DbBundle != null)
                    {
                        var mapping = new BundleChannelMapping
                        {
                            Channel = dbChannel,
                            ChannelID = dbChannel.ID,
                            Bundle = SelectedBundle.DbBundle,
                            BundleID = SelectedBundle.DbBundle.ID,
                        };

                        dbChannel.BundleChannelMappings.Add(mapping);

                        if (SelectedBundle.DbBundle.BundleChannelMappings == null)
                        {
                            SelectedBundle.DbBundle.BundleChannelMappings = new List<BundleChannelMapping> { mapping };
                        }
                        else
                        {
                            SelectedBundle.DbBundle.BundleChannelMappings.Add(mapping);
                        }
                        //await SqlServerBootstrap.AddBundleChannelMapping(mapping);
                        await DbContext.Data.AddBundleChannelMapping(mapping);

                        var channel = new ChannelViewModel { DbChannel = dbChannel, Id = id, Alias = alias, CanBeDeleted = true };
                        Channels.Add(channel);

                        SelectedBundle.Channels.Add(channel);

                        if (!SelectedBundle.DbBundle.ID.Equals(Guid.Empty))
                        {
                            AddChannelToAll(channel);
                        }
                    }
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
            else if (sender is ChannelViewModel item)
            {
                var editChannelClickInfo = new EditChannelDialogViewModel.EditChannelRoutedEventInfo
                {
                    Clicker = item,
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
                                DeleteChannel(removed[i].BundleName, result.OldId, result.OldAlias, i == removed.Length - 1);
                            }

                            added = changed.Where(p => p.IsPresent).ToArray();
                            foreach (var bundle in added)
                            {
                                AddChannel(bundle.BundleName, id, alias, false);
                            }
                        }
                    }
                }
            }
        }

        public async void AddChannel(string bundleName, string id, string? alias, bool addToAll = true)
        {
            var bundle = Bundles.FirstOrDefault(p => p.Text.Equals(bundleName, StringComparison.InvariantCultureIgnoreCase));
            if (bundle?.DbBundle != null)
            {
                //var dbChannel = await SqlServerBootstrap.GetChannel(id);
                var dbChannel = await DbContext.Data.GetChannel(id);

                if (dbChannel == null)
                {
                    dbChannel = new Channel
                    {
                        SlackChannelID = id,
                        Alias = alias,
                        CanBeDeleted = true,
                        BundleChannelMappings = new List<BundleChannelMapping>()
                    };
                    //await SqlServerBootstrap.AddChannel(dbChannel);
                    await DbContext.Data.AddChannel(dbChannel);
                }

                if (dbChannel != null)
                {
                    var mapping = new BundleChannelMapping
                    {
                        Channel = dbChannel,
                        ChannelID = dbChannel.ID,
                        Bundle = bundle.DbBundle,
                        BundleID = bundle.DbBundle.ID,
                    };
                    //await SqlServerBootstrap.AddBundleChannelMapping(mapping);
                    await DbContext.Data.AddBundleChannelMapping(mapping);

                    if (dbChannel.BundleChannelMappings == null)
                    {
                        dbChannel.BundleChannelMappings = new List<BundleChannelMapping> { mapping };
                    }
                    else
                    {
                        dbChannel.BundleChannelMappings.Add(mapping);
                    }

                    if (bundle.DbBundle.BundleChannelMappings == null)
                    {
                        bundle.DbBundle.BundleChannelMappings = new List<BundleChannelMapping> { mapping };
                    }
                    else
                    {
                        bundle.DbBundle.BundleChannelMappings.Add(mapping);
                    }
                }

                var channel = new ChannelViewModel { DbChannel = dbChannel, Id = id, Alias = alias, CanBeDeleted = true };
                bundle.Channels.Add(channel);

                if (SelectedBundle != null && (SelectedBundle.DbBundle == null || SelectedBundle.DbBundle.ID.Equals(Guid.Empty)))
                {
                    //Channels.Add(channel);

                    if (addToAll)
                    {
                        AddChannelToAll(channel);
                    }
                }
                else if (SelectedBundle != null && SelectedBundle.Channels == bundle.Channels && SelectedBundle.Channels != Channels)
                {
                    Channels.Add(channel);
                }
                ChannelBundleViewModel.RefreshChannelCanBeDeletedFromAll(channel);
            }
        }

        public void AddChannelToAll(ChannelViewModel channel)
        {
            var all = Bundles.FirstOrDefault(p => p.DbBundle == null || (p.DbBundle?.ID.Equals(Guid.Empty) ?? false));
            if (all != null && !all.Channels.Any(p => p.DbChannel?.ID.Equals(channel.DbChannel?.ID ?? Guid.Empty) ?? false))
            {
                var clone = (ChannelViewModel)channel.Clone();
                clone.CanBeDeleted = false;
                all.Channels.Add(clone);
            }
        }

        public async Task DeleteChannel(ChannelViewModel channel)
        {
            if (Channels.Contains(channel))
            {
                var bundleId = SelectedBundle?.DbBundle == null ? (Guid?)null : SelectedBundle.DbBundle.ID;

                if (channel.DbChannel != null)
                {
                    //await SqlServerBootstrap.DeleteChannel(channel.DbChannel.ID, bundleId);
                    await DbContext.Data.DeleteChannel(channel.DbChannel.ID, bundleId);
                }
                Channels.Remove(channel);

                if (SelectedBundle != null)
                {
                    var bundle = Bundles.FirstOrDefault(p => (p.DbBundle != null && bundleId.HasValue && p.DbBundle.ID.Equals(bundleId.Value)) ||
                                                        p.Text.Equals(SelectedBundle.Text));
                    if (bundle != null && bundle.Channels.Contains(channel))
                    {
                        bundle.Channels.Remove(channel);
                    }
                }
            }
        }

        public async void DeleteChannel(string bundleName, string channelId, string? channelAlias, bool refreshChannelCanBeDeletedFromAll = true)
        {
            var bundle = Bundles.FirstOrDefault(p => p.DbBundle != null && p.Text.Equals(bundleName, StringComparison.InvariantCultureIgnoreCase));
            if (bundle != null)
            {
                var channel = bundle.Channels.FirstOrDefault(p => p.DbChannel != null && (p.Id.Equals(channelId) ||
                                                                  (!string.IsNullOrWhiteSpace(p.PureAlias) &&
                                                                   !string.IsNullOrWhiteSpace(channelAlias) &&
                                                                   p.PureAlias.Equals(channelAlias, StringComparison.InvariantCultureIgnoreCase))));
                if (channel != null)
                {
                    //await SqlServerBootstrap.DeleteChannel(channel.DbChannel!.ID, bundle.DbBundle!.ID);
                    
                    //bundle.Channels.Remove(channel);

                    if (SelectedBundle != null)
                    {
                        if (SelectedBundle.DbBundle != null && SelectedBundle.DbBundle.ID.Equals(bundle.DbBundle!.ID))
                        {
                            // Delete from selected bundle and update
                            var channel2 = SelectedBundle.Channels.FirstOrDefault(p => p.DbChannel != null && (p.Id.Equals(channel.Id) ||
                                                                                 (!string.IsNullOrWhiteSpace(p.PureAlias) &&
                                                                                  !string.IsNullOrWhiteSpace(channel.PureAlias) &&
                                                                                  p.PureAlias.Equals(channel.PureAlias))));

                            if (channel2 != null)
                            {
                                SelectedBundle.Channels.Remove(channel2);

                                if (SelectedBundle.Channels == bundle.Channels && SelectedBundle.Channels != Channels && Channels.Contains(channel2))
                                {
                                    Channels.Remove(channel2);
                                }

                                if (channel2.DbChannel != null)
                                {
                                    //await SqlServerBootstrap.DeleteChannel(channel2.DbChannel.ID, SelectedBundle.DbBundle.ID);
                                    await DbContext.Data.DeleteChannel(channel2.DbChannel.ID, SelectedBundle.DbBundle.ID);
                                }
                            }
                        }
                        else
                        {
                            // Delete from not selected bundle
                            var channel2 = bundle.Channels.FirstOrDefault(p => p.DbChannel != null && (p.Id.Equals(channel.Id) ||
                                                                          (!string.IsNullOrWhiteSpace(p.PureAlias) &&
                                                                           !string.IsNullOrWhiteSpace(channel.PureAlias) &&
                                                                           p.PureAlias.Equals(channel.PureAlias))));
                            if (channel2 != null && bundle.Channels.Contains(channel2))
                            {
                                bundle.Channels.Remove(channel2);

                                if (channel2.DbChannel != null && bundle.DbBundle != null)
                                {
                                    //await SqlServerBootstrap.DeleteChannel(channel2.DbChannel.ID, bundle.DbBundle.ID);
                                    await DbContext.Data.DeleteChannel(channel2.DbChannel.ID, bundle.DbBundle.ID);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static ChannelViewModel? FindChannelGlobally(string id, string? alias)
        {
            foreach (var item in _channels)
            {
                if (item.Id.Equals(id) || (!string.IsNullOrWhiteSpace(alias) &&
                                           !string.IsNullOrWhiteSpace(item.PureAlias) &&
                                           alias.Equals(item.PureAlias)))
                {
                    return item;
                }
            }
            return null;
        }

        public static async void DeleteChannelGlobally(ChannelViewModel item)
        {
            //var dbChannel = item.DbChannel ?? await SqlServerBootstrap.GetChannel(item.Id);
            var dbChannel = item.DbChannel ?? await DbContext.Data.GetChannel(item.Id);
            if (dbChannel != null)
            {
                //await SqlServerBootstrap.DeleteChannel(dbChannel.ID, null);
                await DbContext.Data.DeleteChannel(dbChannel.ID, null);
            }
            if (_channels.Contains(item))
            {
                _channels.Remove(item);
            }
        }

        public async Task MoveChannelToBundle(ChannelViewModel channel, ChannelBundleViewModel targetBundle)
        {
            if (channel == null || targetBundle == null) return;

            var found = targetBundle.Channels.FirstOrDefault(p => (p.DbChannel != null && channel.DbChannel != null && p.DbChannel.ID.Equals(channel.DbChannel.ID) ||
                                                                  (p.Id.Equals(channel.Id) || (!string.IsNullOrWhiteSpace(p.PureAlias) &&
                                                                                              !string.IsNullOrWhiteSpace(channel.PureAlias) &&
                                                                                              p.PureAlias.Equals(channel.PureAlias)))));

            if (found != null)
            {
                return;
            }

            //if (targetBundle.Channels.Any(p => p.Id.Equals(channel.Id, StringComparison.InvariantCultureIgnoreCase) ||
            //                                   (!string.IsNullOrWhiteSpace(p.PureAlias) &&
            //                                    !string.IsNullOrWhiteSpace(channel.PureAlias) &&
            //                                    p.PureAlias.Equals(channel.PureAlias, StringComparison.InvariantCultureIgnoreCase))))
            //{
            //    return;
            //}

            var dbChannel = channel.DbChannel;
            if (dbChannel == null)
            {
                dbChannel = new Channel
                {
                    SlackChannelID = channel.Id,
                    Alias = channel.PureAlias,
                    CanBeDeleted = channel.CanBeDeleted,
                    BundleChannelMappings = new List<BundleChannelMapping>()
                };
                channel.DbChannel = dbChannel;
                //await SqlServerBootstrap.AddChannel(dbChannel);
                await DbContext.Data.AddChannel(dbChannel);
            }

            var copy = new ChannelViewModel { DbChannel = dbChannel, Id = channel.Id, Alias = channel.Alias };
            targetBundle.Channels.Add(copy);

            Bundle? dbBundle = targetBundle.DbBundle;
            if (dbBundle == null)
            {
                dbBundle = new Bundle
                {
                    Alias = targetBundle.Text,
                    BundleChannelMappings = new List<BundleChannelMapping>()
                };
                targetBundle.DbBundle = dbBundle;
                //await SqlServerBootstrap.AddBundle(dbBundle);
                await DbContext.Data.AddBundle(dbBundle);
            }

            if (dbBundle.BundleChannelMappings == null)
            {
                dbBundle.BundleChannelMappings = new List<BundleChannelMapping>();
            }

            if (dbChannel.BundleChannelMappings == null)
            {
                dbChannel.BundleChannelMappings = new List<BundleChannelMapping>();
            }

            var mapping = new BundleChannelMapping
            {
                Bundle = dbBundle,
                BundleID = dbBundle.ID,
                Channel = dbChannel,
                ChannelID = dbChannel.ID
            };

            dbBundle.BundleChannelMappings.Add(mapping);
            dbChannel.BundleChannelMappings.Add(mapping);
            //await SqlServerBootstrap.AddBundleChannelMapping(mapping);
            await DbContext.Data.AddBundleChannelMapping(mapping);

            ChannelBundleViewModel.RefreshChannelCanBeDeletedFromAll(channel);
        }
    }
}
