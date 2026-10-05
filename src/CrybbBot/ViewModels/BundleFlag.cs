using System;

namespace CrybbBot.ViewModels
{
    public class BundleFlag : ModelBase
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

        public string Alias { get; set; } = string.Empty;
    }
}
