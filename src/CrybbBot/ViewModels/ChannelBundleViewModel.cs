using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using DataLayer.Models;

namespace CrybbBot.ViewModels
{
    public class ChannelBundleViewModel : ModelBase
    {
        public static ObservableCollection<ChannelBundleViewModel>? GlobalCollection { get; set; }

        public ObservableCollection<ChannelViewModel> Channels { get; } = new ObservableCollection<ChannelViewModel>();

        public ChannelBundleViewModel() : base()
        {
            Channels.CollectionChanged += Channels_CollectionChanged;
        }

        private void Channels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is ChannelViewModel channel)
                    {
                        RefreshChannelCanBeDeletedFromAllOnCollectionChanged(channel);
                    }
                }
            }
        }

        private void RefreshChannelCanBeDeletedFromAllOnCollectionChanged(ChannelViewModel channel)
        {
            var canBeDeleted = true;

            if (GlobalCollection != null &&
                Text.Equals(Constants.All, StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var bundle in GlobalCollection)
                {
                    if (bundle == this)
                    {
                        continue;
                    }
                    if (bundle.Channels.Any(p => p.Id.Equals(channel.Id) || (!string.IsNullOrWhiteSpace(p.PureAlias) &&
                                                                             !string.IsNullOrWhiteSpace(channel.PureAlias) &&
                                                                             p.PureAlias.Equals(channel.PureAlias, StringComparison.InvariantCultureIgnoreCase))))
                    {
                        canBeDeleted = false;
                        break;
                    }
                }
            }

            channel.CanBeDeleted = canBeDeleted;
        }

        private static ChannelBundleViewModel? FindAll()
        {
            if (GlobalCollection == null)
            {
                return null;
            }

            ChannelBundleViewModel? all = null;
            foreach (var bundle in GlobalCollection)
            {
                if (bundle.Text.Equals(Constants.All, StringComparison.InvariantCultureIgnoreCase))
                {
                    all = bundle;
                    break;
                }
            }

            return all;
        }

        public static void RefreshChannelCanBeDeletedFromAll(ChannelViewModel channel) =>
            RefreshChannelCanBeDeletedFromAll(channel.Id, channel.PureAlias);

        public static void RefreshChannelCanBeDeletedFromAll(string id, string? alias)
        {
            if (GlobalCollection == null)
            {
                return;
            }

            var all = FindAll();
            if (all == null)
            {
                return;
            }

            ChannelViewModel? copy = null;

            foreach (var item in all.Channels)
            {
                if (item.Id.Equals(id) || (!string.IsNullOrWhiteSpace(item.PureAlias) &&
                                           !string.IsNullOrWhiteSpace(alias) &&
                                           item.PureAlias.Equals(alias)))
                {
                    copy = item;
                    break;
                }
            }

            if (copy == null)
            {
                return;
            }

            var channel = copy;

            var canBeDeleted = true;

            foreach (var bundle in GlobalCollection)
            {
                if (bundle == all)
                {
                    continue;
                }
                if (bundle.Channels.Any(p => p.Id.Equals(channel.Id) || (!string.IsNullOrWhiteSpace(p.PureAlias) &&
                                                                         !string.IsNullOrWhiteSpace(channel.PureAlias) &&
                                                                         p.PureAlias.Equals(channel.PureAlias, StringComparison.InvariantCultureIgnoreCase))))
                {
                    canBeDeleted = false;
                    break;
                }
            }

            channel.CanBeDeleted = canBeDeleted;
        }

        public bool CanBeDeleted { get; set; } = true;
        public bool CanBeModified { get; set; } = true;

        public Bundle? DbBundle { get; set; }

        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                RaisePropertyChanged();
            }
        }

        private bool _highlighted;
        public bool Highlighted
        {
            get => _highlighted;
            set
            {
                _highlighted = value;
                RaisePropertyChanged();
                RaisePropertyChanged("TextFontWeight");
            }
        }
        public FontWeight TextFontWeight => Highlighted ? FontWeights.Black : FontWeights.Normal;
    }
}
