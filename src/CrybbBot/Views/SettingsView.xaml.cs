using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Controls;
using CrybbBot.Enums;
using CrybbBot.ViewModels;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private SettingsViewModel _vm => (SettingsViewModel)Resources["ViewModel"];

        public EventHandler? CancelClicked { get; set; }
        public EventHandler<Models.Settings>? SaveClicked { get; set; }
        
        public SettingsView()
        {
            InitializeComponent();
        }

        public void Reset()
        {
            _vm.BotToken = null;
        }

        public void Init(Models.Settings? settings)
        {
            if (settings == null) return;
            _vm.BotToken = settings.BotToken;
        }

        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var appSettingsPath = Path.Combine(root!, "appsettings.json");

            var jsonLines = File.ReadAllLines(appSettingsPath).Where(p => !p.TrimStart().StartsWith("//"));
            var json = string.Join("\n", jsonLines);

            JsonNode? jsonObj = JsonNode.Parse(json);

            jsonObj["Slack"]["BotToken"] = tbBotToken.Text;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            File.WriteAllText(appSettingsPath, jsonObj.ToJsonString(options));

            SaveClicked?.Invoke(sender, _vm.Settings);
        }

        private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e) => CancelClicked?.Invoke(sender, e);

        public void Focus(SettingsField field = SettingsField.None)
        {
            switch (field)
            {
                case SettingsField.BotToken:
                    tbBotToken.Focus();
                    tbBotToken.SelectAll();
                    break;
                case SettingsField.None:
                default:
                    break;
            }
        }
    }
}
