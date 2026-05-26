using CommunityToolkit.Mvvm.ComponentModel;

namespace YTDownloaderApp.Models;

public partial class SearchResultItem : ObservableObject
{
    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _author = "";

    [ObservableProperty]
    private string _duration = "";

    [ObservableProperty]
    private string _thumbnailUrl = "";

    [ObservableProperty]
    private string _videoUrl = "";
}
