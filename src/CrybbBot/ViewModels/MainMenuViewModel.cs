using System.Reflection;
using System.Windows;

namespace CrybbBot.ViewModels
{
    public class MainMenuViewModel : ModelBase
    {
        public string AppVersion
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly().GetName();
                var ver = assembly.Version;
                return $"{ver?.Major ?? 0}.{ver?.Minor ?? 0}.{ver?.Build ?? 0}";
            }
        }

        private bool _isBotRunning;
        public bool IsBotRunning
        {
            get => _isBotRunning;
            set
            {
                _isBotRunning = value;
                RaisePropertyChanged();
                RaisePropertyChanged("BotIsRunningVisibility");
                RaisePropertyChanged("BotIsNotRunningVisibility");
            }
        }
        public Visibility BotIsRunningVisibility => IsBotRunning ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BotIsNotRunningVisibility => IsBotRunning ? Visibility.Collapsed : Visibility.Visible;

        private bool _isDeveloperToolEnabled;
        public bool IsDeveloperToolEnabled
        {
            get => _isDeveloperToolEnabled;
            set
            {
                _isDeveloperToolEnabled = value;
                RaisePropertyChanged();
            }
        }
    }
}
