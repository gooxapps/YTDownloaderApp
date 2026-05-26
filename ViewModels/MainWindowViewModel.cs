using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeExplode;
using YoutubeExplode.Search;
using YTDownloaderApp.Models;
using Velopack;

namespace YTDownloaderApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private YoutubeDL _ytdl;
    private YoutubeClient _youtubeClient;

    [ObservableProperty]
    private string _inputUrl = "";

    [ObservableProperty]
    private bool _isReady = true;

    [ObservableProperty]
    private bool _isSearching = false;

    [ObservableProperty]
    private string _statusMessage = "Initializing downloader components...";

    [ObservableProperty]
    private int _selectedFormatIndex = 0; // 0=4K, 1=1080p, 2=720p, 3=480p, 4=Audio

    [ObservableProperty]
    private int _selectedTabIndex = 0; // 0 = Search, 1 = Downloads

    [ObservableProperty]
    private string _outputFolderPath = "";

    [ObservableProperty]
    private bool _embedSubtitles = false;

    [ObservableProperty]
    private string _startTime = "";

    [ObservableProperty]
    private string _endTime = "";

    [ObservableProperty]
    private string _updateStatusMessage = "";

    [ObservableProperty]
    private bool _isUpdateAvailable = false;

    private Velopack.UpdateInfo? _updateInfo;

    public ObservableCollection<DownloadItem> Downloads { get; } = new();
    public ObservableCollection<SearchResultItem> SearchResults { get; } = new();

    public MainWindowViewModel()
    {
        _ytdl = new YoutubeDL();
        OutputFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        _ytdl.OutputFolder = OutputFolderPath;
        _youtubeClient = new YoutubeClient();
        
        string localBin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin");
        _ytdl.YoutubeDLPath = Path.Combine(localBin, "yt-dlp");
        _ytdl.FFmpegPath = Path.Combine(localBin, "ffmpeg");
        
        StatusMessage = "Ready";
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(InputUrl)) return;
        
        IsSearching = true;
        SearchResults.Clear();
        StatusMessage = "Searching YouTube...";
        
        try 
        {
            int count = 0;
            await foreach (var batch in _youtubeClient.Search.GetResultBatchesAsync(InputUrl))
            {
                foreach (var result in batch.Items)
                {
                    if (result is VideoSearchResult video)
                    {
                        // Get the highest resolution thumbnail
                        string thumbUrl = "";
                        if (video.Thumbnails != null && video.Thumbnails.Count > 0)
                        {
                            thumbUrl = video.Thumbnails[^1].Url; // Last one is usually highest res
                        }

                        SearchResults.Add(new SearchResultItem
                        {
                            Title = video.Title,
                            Author = video.Author.ChannelTitle,
                            Duration = video.Duration?.ToString(@"hh\:mm\:ss") ?? "",
                            ThumbnailUrl = thumbUrl,
                            VideoUrl = video.Url
                        });
                        
                        count++;
                        if (count >= 50) break;
                    }
                }
                if (count >= 50) break;
            }
            StatusMessage = $"Found {SearchResults.Count} results.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Search failed: " + ex.Message;
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void DownloadSearchResult(SearchResultItem result)
    {
        if (result == null) return;
        if (!IsReady)
        {
            StatusMessage = "Still initializing yt-dlp, please wait...";
            return;
        }

        var item = new DownloadItem(result.VideoUrl)
        {
            Title = result.Title,
            CanCancel = true,
            CanPause = true,
            Status = "Starting..."
        };

        Downloads.Add(item);
        StatusMessage = $"Added '{result.Title}' to downloads.";
        SelectedTabIndex = 1; // Switch to Downloads tab automatically

        _ = StartDownloadTaskAsync(item);
    }

    [RelayCommand]
    private void DownloadAllSearchResults()
    {
        if (!IsReady)
        {
            StatusMessage = "Still initializing yt-dlp, please wait...";
            return;
        }

        int addedCount = 0;
        foreach (var result in SearchResults)
        {
            var item = new DownloadItem(result.VideoUrl)
            {
                Title = result.Title,
                CanCancel = true,
                CanPause = true,
                Status = "Starting..."
            };
            Downloads.Add(item);
            _ = StartDownloadTaskAsync(item);
            addedCount++;
        }

        if (addedCount > 0)
        {
            SelectedTabIndex = 1; // Switch to Downloads tab automatically
            StatusMessage = $"Added {addedCount} videos to downloads.";
        }
    }

    [RelayCommand]
    private void AddDownload()
    {
        if (string.IsNullOrWhiteSpace(InputUrl)) return;
        if (!IsReady)
        {
            StatusMessage = "Still initializing yt-dlp, please wait...";
            return;
        }

        var url = InputUrl.Trim();
        InputUrl = "";

        var item = new DownloadItem(url)
        {
            CanCancel = true,
            CanPause = true,
            Status = "Starting..."
        };

        Downloads.Add(item);
        SelectedTabIndex = 1; // Switch to Downloads tab automatically
        _ = StartDownloadTaskAsync(item);
    }

    [RelayCommand]
    private async Task BrowseFolderAsync(Avalonia.Controls.Window window)
    {
        if (window == null) return;
        var folders = await window.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "Select Output Folder",
            AllowMultiple = false
        });
        if (folders != null && folders.Count > 0)
        {
            OutputFolderPath = folders[0].Path.LocalPath;
            _ytdl.OutputFolder = OutputFolderPath;
        }
    }

    private async Task StartDownloadTaskAsync(DownloadItem item)
    {
        item.IsDownloading = true;
        item.CanPause = true;
        item.CanResume = false;
        item.Status = "Downloading...";
        
        item.CancellationTokenSource = new CancellationTokenSource();

        var progress = new Progress<DownloadProgress>(p =>
        {
            item.ProgressPercentage = Math.Round(p.Progress * 100, 2);
            item.DownloadSpeed = p.DownloadSpeed;
            if (item.IsDownloading)
            {
                item.Status = p.State.ToString();
            }
        });

        try
        {
            if (string.IsNullOrEmpty(item.Title) || item.Title == "Fetching info...")
            {
                var res = await _ytdl.RunVideoDataFetch(item.Url);
                if (res.Success && res.Data != null)
                {
                    item.Title = res.Data.Title;
                }
            }

            RunResult<string> downloadResult;

            var options = new OptionSet();
            if (EmbedSubtitles)
            {
                options.WriteSubs = true;
                options.WriteAutoSubs = true;
                options.EmbedSubs = true;
                options.SubLangs = "all";
            }
            if (!string.IsNullOrWhiteSpace(StartTime) && !string.IsNullOrWhiteSpace(EndTime))
            {
                options.AddCustomOption("--download-sections", $"*{StartTime}-{EndTime}");
                options.ForceKeyframesAtCuts = true; // Ensures the cut is precise
            }

            if (SelectedFormatIndex == 4) // Audio Only
            {
                downloadResult = await _ytdl.RunAudioDownload(
                    item.Url,
                    AudioConversionFormat.Mp3,
                    ct: item.CancellationTokenSource.Token,
                    progress: progress,
                    overrideOptions: options
                );
            }
            else
            {
                string format = SelectedFormatIndex switch
                {
                    1 => "bestvideo[height<=1080]+bestaudio/best",
                    2 => "bestvideo[height<=720]+bestaudio/best",
                    3 => "bestvideo[height<=480]+bestaudio/best",
                    _ => "bestvideo+bestaudio/best" // 4K or fallback
                };

                downloadResult = await _ytdl.RunVideoDownload(
                    item.Url,
                    format: format,
                    mergeFormat: DownloadMergeFormat.Mp4,
                    ct: item.CancellationTokenSource.Token,
                    progress: progress,
                    overrideOptions: options
                );
            }

            if (downloadResult.Success)
            {
                item.Status = "Completed";
                item.ProgressPercentage = 100;
                item.DownloadSpeed = "";
                item.CanPause = false;
                item.CanCancel = false;
            }
            else if (item.CancellationTokenSource.IsCancellationRequested)
            {
                item.Status = "Paused / Cancelled";
                item.CanResume = true;
                item.DownloadSpeed = "";
            }
            else
            {
                item.Status = "Error: " + string.Join(", ", downloadResult.ErrorOutput);
                item.CanResume = true;
            }
        }
        catch (TaskCanceledException)
        {
            item.Status = "Paused / Cancelled";
            item.CanResume = true;
        }
        catch (Exception ex)
        {
            item.Status = $"Error: {ex.Message}";
            item.CanResume = true;
        }
        finally
        {
            item.IsDownloading = false;
            item.CanPause = false;
        }
    }

    [RelayCommand]
    private void PauseDownload(DownloadItem item)
    {
        if (item == null || !item.IsDownloading) return;
        
        item.CancellationTokenSource?.Cancel();
        item.Status = "Pausing...";
        item.CanPause = false;
    }

    [RelayCommand]
    private void ResumeDownload(DownloadItem item)
    {
        if (item == null || item.IsDownloading) return;
        _ = StartDownloadTaskAsync(item);
    }

    [RelayCommand]
    private void CancelDownload(DownloadItem item)
    {
        if (item == null) return;
        if (item.IsDownloading)
        {
            item.CancellationTokenSource?.Cancel();
        }
        Downloads.Remove(item);
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            UpdateStatusMessage = "Checking for updates...";
            // PLACEHOLDER: Replace with your actual GitHub repo URL when ready!
            var mgr = new UpdateManager("https://github.com/gooxapps/YTDownloaderApp");
            
            if (!mgr.IsInstalled)
            {
                UpdateStatusMessage = "App not installed via setup. Cannot auto-update.";
                return;
            }

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                UpdateStatusMessage = "You are on the latest version.";
                IsUpdateAvailable = false;
                return;
            }

            UpdateStatusMessage = $"Update {newVersion.TargetFullRelease.Version} available!";
            _updateInfo = newVersion;
            IsUpdateAvailable = true;
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = $"Update check failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DownloadAndApplyUpdateAsync()
    {
        if (_updateInfo == null) return;
        try
        {
            UpdateStatusMessage = "Downloading update...";
            var mgr = new UpdateManager("https://github.com/gooxapps/YTDownloaderApp");
            await mgr.DownloadUpdatesAsync(_updateInfo, progress => 
            {
                UpdateStatusMessage = $"Downloading... {progress}%";
            });
            
            UpdateStatusMessage = "Applying update and restarting...";
            mgr.ApplyUpdatesAndRestart(_updateInfo);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = $"Update failed: {ex.Message}";
        }
    }
}
