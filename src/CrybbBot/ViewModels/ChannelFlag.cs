using System;

namespace CrybbBot.ViewModels
{
    public sealed class ChannelFlag : ModelBase
    {
        private bool _flag;
        public bool Flag
        {
            get => _flag;
            set
            {
                _flag = value;
                RaisePropertyChanged();
            }
        }

        public Guid ID { get; set; }

        private string? _alias;
        public string? Alias
        {
            get => string.IsNullOrWhiteSpace(_alias) ? ID.ToString() : _alias;
            set => _alias = value;
        }
    }
}
