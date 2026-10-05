using System;
using DataLayer.Models;

namespace CrybbBot.ViewModels
{
    public class ChannelViewModel : ModelBase, ICloneable
    {
        public Channel? DbChannel { get; set; }

        private bool _canBeDeleted = true;
        public bool CanBeDeleted
        {
            get => _canBeDeleted;
            set
            {
                _canBeDeleted = value;
                RaisePropertyChanged();
            }
        }

        private string _id = string.Empty;
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                RaisePropertyChanged();
                RaisePropertyChanged("Alias");
            }
        }

        private string? _alias = string.Empty;
        public string? Alias
        {
            get => _alias ?? Id.ToString();
            set
            {
                _alias = value;
                RaisePropertyChanged();
            }
        }
        public string? PureAlias => _alias;

        public object Clone()
        {
            return new ChannelViewModel
            {
                DbChannel = DbChannel,
                Id = Id,
                Alias = PureAlias,
                CanBeDeleted = CanBeDeleted
            };
        }
    }
}
