using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CrybbBot.Enums;
using CrybbBot.ViewModels;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : UserControl
    {
        private MainMenuViewModel _vm => (MainMenuViewModel)Resources["ViewModel"];

        public EventHandler<MenuPage> MenuItemSelected { get; set; }
        public EventHandler<BotStatus> BotStatusChanged { get; set; }

        public bool IsBotRunning
        {
            get => _vm.IsBotRunning;
            set => _vm.IsBotRunning = value;
        }

        public MainMenu()
        {
            InitializeComponent();
        }

        private void CreateMessage_Click(object sender, RoutedEventArgs e) => MenuItemSelected?.Invoke(sender, MenuPage.NewMessage);

        private void ShowAllMessages_Click(object sender, RoutedEventArgs e) => MenuItemSelected?.Invoke(sender, MenuPage.AllMessages);

        private void ChannelManagement_Click(object sender, RoutedEventArgs e) => MenuItemSelected?.Invoke(sender, MenuPage.ChannelManagement);

        private void ShowSettings_Click(object sender, RoutedEventArgs e) => MenuItemSelected?.Invoke(sender, MenuPage.Settings);

        private void ShowDeveloperTools_Click(object sender, RoutedEventArgs e) => MenuItemSelected?.Invoke(sender, MenuPage.DeveloperTools);

        private void StartBot_Click(object sender, RoutedEventArgs e) => BotStatusChanged?.Invoke(sender, BotStatus.Running);

        private void StopBot_Click(object sender, RoutedEventArgs e) => BotStatusChanged?.Invoke(sender, BotStatus.Stopped);

        private bool _isMouseOverAndDown { get; set; }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isMouseOverAndDown)
            {
                return;
            }
            _isMouseOverAndDown = true;
            Task.Run(WaitForDevekioerToolsUnlocked);
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e) => _isMouseOverAndDown = false;

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseOverAndDown = false;
        }

        private async Task WaitForDevekioerToolsUnlocked()
        {
            var sw = Stopwatch.StartNew();
            while (_isMouseOverAndDown && sw.Elapsed.Milliseconds < 250)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
            sw.Reset();

            if (_isMouseOverAndDown)
            {
                _vm.IsDeveloperToolEnabled = !_vm.IsDeveloperToolEnabled;
                if (_vm.IsDeveloperToolEnabled)
                {
                    MainWindow.ShowDeveloperTools();
                }
            }

            _isMouseOverAndDown = false;
        }
    }
}
