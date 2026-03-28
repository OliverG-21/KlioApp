using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KlioApp.Core.Interfaces;
using KlioApp.Core.Models;

namespace KlioApp.UI.ViewModels;

public partial class UploadItemViewModel : ViewModelBase
{
    [ObservableProperty] private UploadStatus _status = UploadStatus.Pending;
    [ObservableProperty] private string? _errorMessage;
    public string FilePath { get; init; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public string TargetAlbumName { get; init; } = string.Empty;
    public int TargetAlbumId { get; init; }
    public List<string> Tags { get; } = [];

    public void SetTags(List<string> tags)
    {
        Tags.Clear();
        Tags.AddRange(tags);
        OnPropertyChanged(nameof(TagSummary));
        OnPropertyChanged(nameof(TagCount));
    }

    public string TagSummary => Tags.Count > 0 ? string.Join(", ", Tags) : string.Empty;
    public int TagCount => Tags.Count;

    public string StatusText => Status switch
    {
        UploadStatus.Pending => "Wartend",
        UploadStatus.Uploading => "Lädt hoch...",
        UploadStatus.Done => "Fertig",
        UploadStatus.Failed => $"Fehler: {ErrorMessage}",
        _ => string.Empty
    };
}

public partial class UploadViewModel : ViewModelBase
{
    private readonly IKlioApiClient _apiClient;

    [ObservableProperty] private bool _isUploading;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private bool _hasPendingItems;

    private ObservableCollection<UploadItemViewModel> _queue = [];
    public ObservableCollection<UploadItemViewModel> Queue
    {
        get => _queue;
        private set
        {
            _queue = value;
            _queue.CollectionChanged += (_, _) => UpdateHasPending();
            UpdateHasPending();
            OnPropertyChanged();
        }
    }

    public UploadViewModel(IKlioApiClient apiClient)
    {
        _apiClient = apiClient;
        _queue.CollectionChanged += (_, _) => UpdateHasPending();
    }

    private void UpdateHasPending() =>
        HasPendingItems = _queue.Any(i => i.Status == UploadStatus.Pending);

    public void AddFiles(List<string> filePaths, AlbumInfo targetAlbum)
    {
        foreach (var path in filePaths)
        {
            Queue.Add(new UploadItemViewModel
            {
                FilePath = path,
                TargetAlbumId = targetAlbum.Id,
                TargetAlbumName = targetAlbum.Name
            });
        }
    }

    [RelayCommand]
    public void ClearCompleted()
    {
        var done = Queue.Where(i => i.Status == UploadStatus.Done).ToList();
        foreach (var item in done) Queue.Remove(item);
    }

    [RelayCommand]
    public void ClearAll() => Queue.Clear();

    [RelayCommand]
    public async Task StartUploadAsync()
    {
        var pending = Queue.Where(i => i.Status == UploadStatus.Pending).ToList();
        if (pending.Count == 0) return;

        IsUploading = true;
        int done = 0;

        foreach (var item in pending)
        {
            item.Status = UploadStatus.Uploading;
            StatusText = $"Lädt hoch: {item.FileName}";
            try
            {
                await _apiClient.UploadImageAsync(
                    item.FilePath,
                    item.TargetAlbumId,
                    item.Tags,
                    new Progress<int>());
                item.Status = UploadStatus.Done;
            }
            catch (Exception ex)
            {
                item.Status = UploadStatus.Failed;
                item.ErrorMessage = ex.Message;
            }
            done++;
            Progress = (int)(done * 100.0 / pending.Count);
            UpdateHasPending();
        }

        StatusText = $"{done} Dateien hochgeladen";
        IsUploading = false;
    }
}
