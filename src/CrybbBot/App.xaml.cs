using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using NLog;

namespace CrybbBot;

public partial class App : Application
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected override void OnStartup(StartupEventArgs e)
    {
        Logger.Info("Application starting...");
        base.OnStartup(e);

        var primary = Colors.Indigo;
        var secondary = Colors.Lime;

        var paletteHelper = new PaletteHelper();

        // v5.x: GetTheme() returns Theme
        Theme theme = paletteHelper.GetTheme();

        theme.SetBaseTheme(BaseTheme.Dark);
        theme.SetPrimaryColor(primary);
        theme.SetSecondaryColor(secondary);

        paletteHelper.SetTheme(theme);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Application exiting...");
        LogManager.Shutdown();
        base.OnExit(e);
    }
}