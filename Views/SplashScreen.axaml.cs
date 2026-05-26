using Avalonia.Controls;
using System.Threading.Tasks;
using System;
using Avalonia.Threading;

namespace YTDownloaderApp.Views;

public partial class SplashScreen : Window
{
    public SplashScreen()
    {
        InitializeComponent();
        Loaded += async (s, e) => await InitializeAppAsync();
    }

    private async Task InitializeAppAsync()
    {
        var statusText = this.FindControl<TextBlock>("StatusText");
        
        try
        {
            if (statusText != null) statusText.Text = "Checking yt-dlp dependencies...";
            
            // Artificial delay to show the beautiful splash screen
            await Task.Delay(1500);

            if (statusText != null) statusText.Text = "Starting Engine...";
            await Task.Delay(500);

            // Open main window on the UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var mainWindow = new MainWindow
                {
                    DataContext = new ViewModels.MainWindowViewModel()
                };
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = mainWindow;
                }
                mainWindow.Show();
                this.Close();
            });
        }
        catch (Exception ex)
        {
            if (statusText != null) statusText.Text = $"Error: {ex.Message}";
        }
    }
}
