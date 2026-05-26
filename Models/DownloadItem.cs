using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YTDownloaderApp.Models;

public partial class DownloadItem : ObservableObject
{
    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _title = "Fetching info...";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _downloadSpeed = "";

    [ObservableProperty]
    private string _status = "Pending";

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private bool _canPause;

    [ObservableProperty]
    private bool _canResume;

    [ObservableProperty]
    private bool _canCancel = true;

    public CancellationTokenSource CancellationTokenSource { get; set; } = new();

    // Store process ID if we need to kill it
    public int ProcessId { get; set; }

    public DownloadItem(string url)
    {
        Url = url;
    }
}
