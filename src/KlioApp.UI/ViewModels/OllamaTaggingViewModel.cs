using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KlioApp.Core.Interfaces;
using KlioApp.Core.Models;

namespace KlioApp.UI.ViewModels;

public partial class OllamaTaggingViewModel : ViewModelBase
{
    private readonly IOllamaClient _ollamaClient;
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private bool _isTagging;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private bool _isAvailable;

    public OllamaTaggingViewModel(IOllamaClient ollamaClient, ISettingsService settingsService)
    {
        _ollamaClient = ollamaClient;
        _settingsService = settingsService;
    }

    public void ConfigureFromSettings()
    {
        var s = _settingsService.Load();
        _ollamaClient.Configure(s.OllamaUrl, s.OllamaModel, string.Empty);
    }

    [RelayCommand]
    public async Task CheckAvailabilityAsync()
    {
        ConfigureFromSettings();
        IsAvailable = await _ollamaClient.IsAvailableAsync();
        StatusText = IsAvailable ? "Ollama erreichbar" : "Ollama nicht erreichbar";
    }

    // Tags all pending items in the upload queue
    public async Task TagQueueItemsAsync(IReadOnlyList<UploadItemViewModel> pendingItems)
    {
        if (pendingItems.Count == 0) return;

        ConfigureFromSettings();
        IsTagging = true;
        Progress = 0;
        int done = 0;

        using var cts = new CancellationTokenSource();

        foreach (var item in pendingItems)
        {
            if (item.Status != UploadStatus.Pending) { done++; continue; }

            StatusText = $"Tagge: {item.FileName} ({done + 1}/{pendingItems.Count})";
            try
            {
                var tags = await _ollamaClient.TagImageAsync(item.FilePath, cts.Token);
                item.SetTags(tags);
            }
            catch (OperationCanceledException) { break; }
            catch
            {
                // Skip on error, don't block the batch
            }
            done++;
            Progress = (int)(done * 100.0 / pendingItems.Count);
        }

        StatusText = $"{done} Bilder getaggt";
        IsTagging = false;
    }
}
