using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CrybbBot.Enums;
using CrybbBot.Helpers;
using CrybbBot.Models;
using CrybbBot.Services;
using CrybbBot.ViewModels;
using CrybbBot.Views.Dialogs;
using DataLayer;
using DataLayer.Enums;
using DataLayer.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using SlackBotSender.Exceptions;
using static CrybbBot.ViewModels.EditChannelDialogViewModel;

namespace CrybbBot;

public partial class MainWindow : Window, IDisposable
{
    private MainWindowViewModel _vm => (MainWindowViewModel)Resources["ViewModel"];

    private readonly DialogService _dialogService = new("RootDialog");

    private static MainWindow _instance { get; set; }

    private static Settings _settings { get; set; } = new Settings();

    public MainWindow()
    {
        _instance = this;

        _ = Init();
    }

    private async Task Init()
    {
        InitializeComponent();

        HeavyTaskManager.SetMainWindowViewModel(_vm);

        await InitSQLite();

        _mainMenuView.MenuItemSelected = OnMenuItemSelected;
        _mainMenuView.BotStatusChanged = BotStatusChanged_Click;
        _settingsView.CancelClicked = ShowMainMenu_Click;
        _settingsView.SaveClicked = SaveSettings_Click;

        var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var appSettingsPath = Path.Combine(root, "appsettings.json");

        if (!File.Exists(appSettingsPath))
        {
            var defaultAppSettingsPath = Path.Combine(root, "default_appsettings.json");
            if (File.Exists(defaultAppSettingsPath))
            {
                File.Copy(defaultAppSettingsPath, appSettingsPath);
            }
        }

        var config = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: true)
            .Build();

        _settings.BotToken = config["Slack:BotToken"];
        _settingsView.Init(_settings);

        LoadChannels();
    }

    public void Dispose()
    {
        if (_mainMenuView.IsBotRunning)
        {
            if (SlackBotSender.Client.IsInitialized)
            {
                SlackBotSender.Client.Finalize();
            }
            _mainMenuView.IsBotRunning = false;
        }
    }

    private static async Task InitSQLite()
    {
        var oAssembly = Assembly.GetExecutingAssembly();
        var root = Path.GetDirectoryName(oAssembly.Location)!;
        var dbFilePath = Path.Combine(root, "DB", "app.db");

        var dir = Path.GetDirectoryName(dbFilePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        // 2) Opening connection creates the DB file if missing
        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = dbFilePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        DbContext.Init(DbKind.Sqlite, cs);

        await SqliteConnect();
    }

    private static async Task SqliteConnect()
    {
        try
        {
            await HeavyTaskManager.DoHeavyWorkAsync("Під’єднання до бази даних...", async () =>
            {
                var oAssembly = Assembly.GetExecutingAssembly();
                var root = Path.GetDirectoryName(oAssembly.Location)!;
                var dbDir = Path.Combine(root, "DB");
                Directory.CreateDirectory(dbDir);
                var dbFilePath = Path.Combine(dbDir, "app.db");
                var sqlite = new DbConnectionFactory(DbKind.Sqlite, $"Data Source={dbFilePath};");

                sqlite?.Create();
                await SqliteBootstrap.EnsureDbAsync();
            });
        }
        catch (Exception ex)
        {
            await MainWindow.ShowDialog("Error", ex.Message, false);
        }
    }

    public static async Task ShowDialog(string title, string message, bool isReadonly = true)
    {
        try
        {
            if (_instance?._vm == null)
            {
                return;
            }

            var dialog = new MessageDialogView
            {
                DataContext = new MessageDialogViewModel(title, message, isReadonly)
            };
            _instance._vm.RegisterDialog(dialog);

            await _instance._dialogService.ShowAsync(dialog);
            _instance._vm.UnregisterDialog();
        }
        catch { }
    }

    public static async Task<bool> ShowYesNoDialog(string title, string message)
    {
        try
        {
            var dialog = new YesNoDialogView
            {
                DataContext = new YesNoDialogViewModel(title, message)
            };
            _instance._vm.RegisterDialog(dialog);

            var result = await _instance._dialogService.ShowAsync(dialog);
            _instance._vm.UnregisterDialog();

            if (bool.TryParse(result?.ToString() ?? string.Empty, out var res))
            {
                return res;
            }
            return false;
        }
        catch { }

        return false;
    }

    public static async Task<InputDialogResult> ShowInputDialog(string? title, string? message) =>
        await ShowInputDialog(0, title, message);

    public static async Task<InputDialogResult> ShowInputDialog(int maxLength, string? title, string? message)
    {
        try
        {
            var dc = new InputDialogViewModel(maxLength, title, message);
            var dialog = new InputDialogView
            {
                DataContext = dc
            };
            _instance._vm.RegisterDialog(dialog);

            dc.FocusTextBox = false;
            var resultTask = _instance._dialogService.ShowAsync(dialog);
            await Task.Yield(); // let UI create bindings
            dc.FocusTextBox = true;

            var result = await resultTask;
            _instance._vm.UnregisterDialog();

            if (result is InputDialogResult res)
            {
                return res;
            }
        }
        catch { }

        return new InputDialogResult
        {
            Yes = false,
            Value = null
        };
    }

    public static async Task<RenameDialogResult> ShowRenameDialog(string title, string oldValue, Tuple<string, string>? captions = null) =>
        await ShowRenameDialog(0, title, oldValue, captions);

    public static async Task<RenameDialogResult> ShowRenameDialog(int maxLength, string title, string oldValue, Tuple<string, string>? captions = null)
    {
        try
        {
            var dc = new RenameDialogViewModel(maxLength, title, oldValue, captions);
            var dialog = new RenameDialogView
            {
                DataContext = dc
            };
            _instance._vm.RegisterDialog(dialog);

            dc.FocusNewValue = false;
            var resultTask = _instance._dialogService.ShowAsync(dialog);
            await Task.Yield(); // let UI create bindings
            dc.FocusNewValue = true;

            var result = await resultTask;
            _instance._vm.UnregisterDialog();

            if (result is RenameDialogResult res)
            {
                return res;
            }
        }
        catch { }

        return new RenameDialogResult
        {
            Yes = false,
            NewValue = null
        };
    }

    public static async Task<AddChannelDialogResult> ShowAddChannelDialog(string? title = null) =>
        await ShowAddChannelDialog(0, title);

    public static async Task<AddChannelDialogResult> ShowAddChannelDialog(int maxLength, string? title)
    {
        try
        {
            var dc = new AddChannelDialogViewModel(maxLength, title);
            var dialog = new AddChannelDialogView
            {
                DataContext = dc
            };
            _instance._vm.RegisterDialog(dialog);

            dc.FocusTextBox = false;
            var resultTask = _instance._dialogService.ShowAsync(dialog);
            await Task.Yield(); // let UI create bindings
            dc.FocusTextBox = true;

            var result = await resultTask;
            _instance._vm.UnregisterDialog();

            if (result is AddChannelDialogResult res)
            {
                return res;
            }
        }
        catch { }

        return new AddChannelDialogResult
        {
            Yes = false,
            Alias = null
        };
    }

    public static async Task<EditChannelDialogResult> ShowEditChannelDialog(string oldId, string? oldAlias, EditChannelRoutedEventInfo? editChannelClickInfo = null) =>
        await ShowEditChannelDialog(0, oldId, oldAlias, editChannelClickInfo);

    public static async Task<EditChannelDialogResult> ShowEditChannelDialog(int maxLength, string oldId, string? oldAlias, EditChannelRoutedEventInfo? editChannelClickInfo = null)
    {
        try
        {
            var dc = new EditChannelDialogViewModel(maxLength, oldId, oldAlias)
            {
                EditChannelRoutedEvent = editChannelClickInfo
            };
            var dialog = new EditChannelDialogView
            {
                DataContext = dc
            };
            _instance._vm.RegisterDialog(dialog);

            dc.FocusNewId = false;
            var resultTask = _instance._dialogService.ShowAsync(dialog);
            await Task.Yield(); // let UI create bindings
            dc.FocusNewId = true;

            var result = await resultTask;
            _instance._vm.UnregisterDialog();

            if (result is EditChannelDialogResult res)
            {
                return res;
            }
        }
        catch { }

        return new EditChannelDialogResult
        {
            Yes = false,
            NewId = null
        };
    }

    public static async Task<EditChannelDialogResult> ShowEditChannelDialog(EditChannelDialogViewModel viewModel)
    {
        try
        {
            var dialog = new EditChannelDialogView
            {
                DataContext = viewModel
            };
            _instance._vm.RegisterDialog(dialog);

            var result = await _instance._dialogService.ShowAsync(dialog);
            _instance._vm.UnregisterDialog();

            if (result is EditChannelDialogResult res)
            {
                return res;
            }
        }
        catch { }

        return new EditChannelDialogResult
        {
            Yes = false,
            NewId = null
        };
    }

    public static async Task<FileReferenceDialogResult> ShowFileReferenceDialog() =>
        await ShowFileReferenceDialog(0);

    public static async Task<FileReferenceDialogResult> ShowFileReferenceDialog(int maxLength)
    {
        try
        {
            var dialog = new FileReferenceDialogView
            {
                DataContext = new FileReferenceDialogViewModel(maxLength)
            };
            _instance._vm.RegisterDialog(dialog);

            var result = await _instance._dialogService.ShowAsync(dialog);
            _instance._vm.UnregisterDialog();

            if (result is FileReferenceDialogResult res)
            {
                return res;
            }
        }
        catch { }

        return new FileReferenceDialogResult
        {
            Yes = false,
            Title = null,
            Url = null
        };
    }

    public static async Task<FileReferenceDialogResult> ShowEditFileReferenceDialog(string title, string url) =>
        await ShowEditFileReferenceDialog(0, title, url);

    public static async Task<FileReferenceDialogResult> ShowEditFileReferenceDialog(int maxLength, string title, string url)
    {
        try
        {
            var dc = new FileReferenceDialogViewModel(maxLength, title, url);
            var dialog = new FileReferenceDialogView
            {
                DataContext = dc
            };
            _instance._vm.RegisterDialog(dialog);

            dc.FocusTitle = false;
            var resultTask = _instance._dialogService.ShowAsync(dialog);
            await Task.Yield(); // let UI create bindings
            dc.FocusTitle = true;

            var result = await resultTask;
            _instance._vm.UnregisterDialog();

            if (result is FileReferenceDialogResult res)
            {
                return res;
            }
        }
        catch { }

        return new FileReferenceDialogResult
        {
            Yes = false,
            Title = null,
            Url = null
        };
    }

    public static async Task<LinkDialogResult> ShowLinkDialog(string url, bool preview = false)
    {
        try
        {
            var dc = new LinkDialogViewModel(url, preview);
            var dialog = new LinkDialogView
            {
                DataContext = dc
            };
            _instance._vm.RegisterDialog(dialog);

            dc.FocusTitle = false;
            var resultTask = _instance._dialogService.ShowAsync(dialog);
            await Task.Yield(); // let UI create bindings
            dc.FocusTitle = true;

            var result = await resultTask;
            _instance._vm.UnregisterDialog();

            if (result is LinkDialogResult res)
            {
                return res;
            }
        }
        catch { }

        return new LinkDialogResult
        {
            Yes = false,
            Url = null
        };
    }

    public static async Task<MessagesFilterDialogResult> ShowMessagesFilterDialog(MessagesFilter? filter)
    {
        try
        {
            var dc = new MessagesFilterDialogViewModel(filter);
            var dialog = new MessagesFilterDialogView
            {
                DataContext = dc
            };
            _instance._vm.RegisterDialog(dialog);

            var resultTask = _instance._dialogService.ShowAsync(dialog);
            await Task.Yield(); // let UI create bindings

            var result = await resultTask;
            _instance._vm.UnregisterDialog();

            if (result is MessagesFilterDialogResult res)
            {
                return res;
            }
        }
        catch { }

        return new MessagesFilterDialogResult
        {
            Yes = false,
            Filter = null
        };
    }

    public static void LoadChannels() => _instance._channelManagementView.Load();
    
    public static void ShowDeveloperTools() => _instance._vm.TransitionerSelectedSlide = 5;

    public static async Task EditMessage(Guid messageId)
    {
        _instance.OnMenuItemSelected(null, MenuPage.NewMessage);

        var result = await _instance._newMessageView.StartEditing(messageId);
        if (!result)
        {
            _instance.OnMenuItemSelected(null, MenuPage.AllMessages);
            await ShowDialog("Помилка", "Не вдалося відкрити повідомлення.");
        }
    }

    public static void ShowAllMessages() => _instance.OnMenuItemSelected(null, MenuPage.AllMessages);

    private void ShowMainMenu_Click(object sender, RoutedEventArgs e) => ShowMainMenu();

    private void ShowMainMenu_Click(object? sender, EventArgs e) => ShowMainMenu();
    
    private void SaveSettings_Click(object? sender, Settings e)
    {
        _settings = e;
        ShowMainMenu();
    }

    private void ShowMainMenu(bool forceMainMenu = false)
    {
        btnsPnl.Free();
        if (!forceMainMenu && _vm.TransitionerSelectedSlide == 1 && _newMessageView.IsEditing)
        {
            OnMenuItemSelected(null, MenuPage.AllMessages);
        }
        else
        {
            _vm.TransitionerSelectedSlide = 0;
        }
    }

    private async void OnMenuItemSelected(object sender, MenuPage page)
    {
        switch (page)
        {
            case MenuPage.NewMessage:
                if (_newMessageView.IsEditing)
                {
                    _newMessageView.Reset();
                }
                _newMessageView.LoadChannels();
                _vm.TransitionerSelectedSlide = 1;

                btnsPnl.SetPannel(_newMessageView.ButtonsPannel, _newMessageView.DataContext);

                _newMessageView.Focus();
                break;
            case MenuPage.AllMessages:
                await HeavyTaskManager.DoHeavyWorkAsync("Завантаження...", async () =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    await Dispatcher.InvokeAsync(() =>
                    {
                        _allMessagesView.Load();
                        btnsPnl.SetPannel(_allMessagesView.ButtonsPannel, _allMessagesView.DataContext);
                        _vm.TransitionerSelectedSlide = 2;
                    },
                    System.Windows.Threading.DispatcherPriority.Normal);
                });
                break;
            case MenuPage.ChannelManagement:
                _channelManagementView.Load();
                _vm.TransitionerSelectedSlide = 3;
                break;
            case MenuPage.Settings:
                _settingsView.Init(_settings);
                _vm.TransitionerSelectedSlide = 4;
                break;
            case MenuPage.DeveloperTools:
                _vm.TransitionerSelectedSlide = 5;
                break;
        }
    }

    private async void BotStatusChanged_Click(object sender, BotStatus e) => await _instance.ChangeState(e);

    public static async Task<bool> ManageBotState(BotStatus e) => await _instance.ChangeState(e);

    private async Task<bool> ChangeState(BotStatus e)
    {
        if (string.IsNullOrWhiteSpace(_settings?.BotToken))
        {
            await ShowDialog("Помилка", "Не вказано Bot User OAuth Token.");
            _settingsView.Init(_settings);
            _vm.TransitionerSelectedSlide = 4;
            _settingsView.Focus(SettingsField.BotToken);
            return false;
        }

        var msg = e == BotStatus.Running
            ? "Запуск бота..."
            : "Призупинення роботи бота...";

        try
        {
            await HeavyTaskManager.DoHeavyWorkAsync(msg, async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                if (_mainMenuView.IsBotRunning)
                {
                    if (SlackBotSender.Client.IsInitialized)
                    {
                        SlackBotSender.Client.Finalize();
                    }
                    _mainMenuView.IsBotRunning = false;
                }
                else
                {
                    await SlackBotSender.Client.Init(_settings.BotToken);
                    _mainMenuView.IsBotRunning = true;
                }
            });

            return true;
        }
        catch (InvalidOperationException)
        {
            await ShowDialog(string.Empty, "Помилка аутентифікації\r\nПеревірте налаштування параметра Bot User OAuth Token.");
            _settingsView.Init(_settings);
            _vm.TransitionerSelectedSlide = 4;
            _settingsView.Focus(SettingsField.BotToken);
        }
        catch (ClientNotInitializedException)
        {
            await ShowDialog("Помилка", "Бот не запущений");
        }
        catch (ClientAlreadyInitializedException)
        {
            await ShowDialog("Помилка", "Бот вже запущений");
        }
        catch (Exception ex)
        {
            await ShowDialog("Помилка", ex.Message);
        }
        
        return false;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            _vm.WindowWidth = fe.ActualWidth;
            _vm.WindowHeight = fe.ActualHeight;
        }
    }
}