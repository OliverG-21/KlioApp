using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KlioApp.Core.Interfaces;
using KlioApp.Core.Models;

namespace KlioApp.UI.ViewModels;

public partial class FileItemViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isSelected;
    public string FilePath { get; init; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public long FileSize { get; init; }
    public string FileSizeText => FileSize > 1_048_576
        ? $"{FileSize / 1_048_576.0:F1} MB"
        : $"{FileSize / 1024.0:F0} KB";
    public DateTime LastModified { get; init; }
}

public partial class FileBrowserViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".heic", ".heif", ".gif", ".webp", ".mp4", ".mov" };

    [ObservableProperty] private string _currentFolder = string.Empty;
    [ObservableProperty] private ObservableCollection<FileItemViewModel> _files = [];
    [ObservableProperty] private int _selectedCount;

    public FileBrowserViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        var settings = settingsService.Load();
        if (!string.IsNullOrEmpty(settings.LastLocalFolder))
            _ = OpenFolderAsync(settings.LastLocalFolder);
    }

    [RelayCommand]
    public async Task BrowseFolderAsync()
    {
        // Folder picker is triggered from the View via TopLevel.StorageProvider
        // The View calls OpenFolderAsync directly after picking
        await Task.CompletedTask;
    }

    public async Task OpenFolderAsync(string path)
    {
        if (!Directory.Exists(path)) return;
        CurrentFolder = path;

        var settings = _settingsService.Load();
        settings.LastLocalFolder = path;
        _settingsService.Save(settings);

        var items = await Task.Run(() =>
            Directory.EnumerateFiles(path)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)))
                .OrderBy(f => f)
                .Select(f => new FileItemViewModel
                {
                    FilePath = f,
                    FileSize = new FileInfo(f).Length,
                    LastModified = File.GetLastWriteTime(f)
                })
                .ToList());

        foreach (var item in items)
            item.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(FileItemViewModel.IsSelected)) UpdateSelectedCount(); };

        Files = new ObservableCollection<FileItemViewModel>(items);
        SelectedCount = 0;
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var f in Files) f.IsSelected = true;
        SelectedCount = Files.Count;
    }

    [RelayCommand]
    public void SelectNone()
    {
        foreach (var f in Files) f.IsSelected = false;
        SelectedCount = 0;
    }

    public void UpdateSelectedCount() =>
        SelectedCount = Files.Count(f => f.IsSelected);

    public List<string> GetSelectedFilePaths() =>
        Files.Where(f => f.IsSelected).Select(f => f.FilePath).ToList();
}
