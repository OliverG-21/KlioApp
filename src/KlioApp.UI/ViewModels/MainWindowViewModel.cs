using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KlioApp.Core.Interfaces;
using KlioApp.Core.Models;

namespace KlioApp.UI.ViewModels;

public record LogEntry(DateTime Timestamp, string Message)
{
    public string Display => $"[{Timestamp:HH:mm:ss}] {Message}";
}

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private bool _showSettings;
    [ObservableProperty] private bool _canAddToQueue;

    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    public ConnectionViewModel ConnectionVM { get; }
    public AlbumTreeViewModel AlbumTreeVM { get; }
    public FileBrowserViewModel FileBrowserVM { get; }
    public UploadViewModel UploadVM { get; }
    public OllamaTaggingViewModel OllamaVM { get; }

    public MainWindowViewModel(
        ConnectionViewModel connectionVM,
        AlbumTreeViewModel albumTreeVM,
        FileBrowserViewModel fileBrowserVM,
        UploadViewModel uploadVM,
        OllamaTaggingViewModel ollamaVM)
    {
        ConnectionVM = connectionVM;
        AlbumTreeVM = albumTreeVM;
        FileBrowserVM = fileBrowserVM;
        UploadVM = uploadVM;
        OllamaVM = ollamaVM;

        AlbumTreeVM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AlbumTreeViewModel.SelectedNode))
                UpdateCanAddToQueue();
        };
        FileBrowserVM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FileBrowserViewModel.SelectedCount))
                UpdateCanAddToQueue();
        };

        OllamaVM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OllamaTaggingViewModel.StatusText)
                && !string.IsNullOrEmpty(OllamaVM.StatusText))
                AddLog(OllamaVM.StatusText);
        };

        UploadVM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UploadViewModel.StatusText)
                && !string.IsNullOrEmpty(UploadVM.StatusText))
                AddLog(UploadVM.StatusText);
        };
    }

    public void AddLog(string message)
    {
        LogEntries.Insert(0, new LogEntry(DateTime.Now, message));
        if (LogEntries.Count > 200)
            LogEntries.RemoveAt(LogEntries.Count - 1);
    }

    private void UpdateCanAddToQueue() =>
        CanAddToQueue = AlbumTreeVM.GetSelectedAlbum() != null && FileBrowserVM.SelectedCount > 0;

    [RelayCommand]
    public async Task RefreshAlbumsAsync()
    {
        await AlbumTreeVM.LoadAsync();
    }

    [RelayCommand]
    public void AddToUploadQueue()
    {
        var album = AlbumTreeVM.GetSelectedAlbum();
        if (album == null) return;
        var files = FileBrowserVM.GetSelectedFilePaths();
        if (files.Count == 0) return;
        UploadVM.AddFiles(files, album);
        FileBrowserVM.SelectNone();
        AddLog($"{files.Count} Datei(en) zu \"{album.Name}\" hinzugefügt");
    }

    [RelayCommand]
    public async Task AutoTagQueueAsync()
    {
        var pending = UploadVM.Queue.Where(i => i.Status == UploadStatus.Pending).ToList();
        await OllamaVM.TagQueueItemsAsync(pending);
    }

    [RelayCommand]
    public void ToggleSettings() => ShowSettings = !ShowSettings;
}
