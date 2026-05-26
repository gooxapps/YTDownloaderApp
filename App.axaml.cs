using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using YTDownloaderApp.ViewModels;
using YTDownloaderApp.Views;

namespace YTDownloaderApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new SplashScreen();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void TrayShow_Click(object? sender, System.EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is not SplashScreen)
            {
                desktop.MainWindow?.Show();
            }
        }
    }

    public void TrayExit_Click(object? sender, System.EventArgs e)
    {
        System.Environment.Exit(0);
    }
}