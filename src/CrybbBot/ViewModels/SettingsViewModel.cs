namespace CrybbBot.ViewModels
{
    public class SettingsViewModel : ModelBase
    {
        private string? _botToken;
        public string? BotToken
        {
            get => _botToken;
            set
            {
                _botToken = value;
                RaisePropertyChanged();
            }
        }

        public Models.Settings Settings
        {
            get
            {
                return new Models.Settings()
                {
                    BotToken = _botToken
                };
            }
        }
    }
}
