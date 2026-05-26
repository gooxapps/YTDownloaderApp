# How to Configure the Auto-Updater

To make the "Check for Updates" button work, you need to point the app to the online location where your updates are hosted (usually a GitHub repository).

Right now, the app is pointing to a placeholder: `https://github.com/USERNAME/YTDownloaderApp`. 

### Steps to replace this with your actual URL:

1. Open the file located at: `ViewModels/MainWindowViewModel.cs`
2. Scroll down to **Line 363**, inside the `CheckForUpdatesAsync()` method. Look for:
   ```csharp
   var mgr = new UpdateManager("https://github.com/USERNAME/YTDownloaderApp");
   ```
3. Change `"https://github.com/USERNAME/YTDownloaderApp"` to the link of your actual GitHub repository.
4. Scroll further down to **Line 394**, inside the `DownloadAndApplyUpdateAsync()` method. Look for the exact same line:
   ```csharp
   var mgr = new UpdateManager("https://github.com/USERNAME/YTDownloaderApp");
   ```
5. Change that link as well to match the one you entered above.
6. Save the file.

### Testing it out
Once you have pushed your code to GitHub and created a new "Release" containing your Velopack installer files, the app will automatically check that URL and find the updates!
