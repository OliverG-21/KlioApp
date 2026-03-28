using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KlioApp.Core.Interfaces;
using KlioApp.Core.Models;

namespace KlioApp.UI.ViewModels;

public partial class ConnectionViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IKlioApiClient _apiClient;

    [ObservableProperty] private string _serverUrl = string.Empty;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _ollamaUrl = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isTesting;

    public ConnectionViewModel(ISettingsService settingsService, IKlioApiClient apiClient)
    {
        _settingsService = settingsService;
        _apiClient = apiClient;
        var settings = settingsService.Load();
        ServerUrl = settings.ServerUrl;
        ApiKey = settings.ApiKey;
        OllamaUrl = settings.OllamaUrl;
    }

    [RelayCommand]
    public async Task TestAndSaveAsync()
    {
        IsTesting = true;
        StatusMessage = "Verbinde...";
        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);
            IsConnected = await _apiClient.TestConnectionAsync();
            StatusMessage = IsConnected ? "Verbunden" : "Verbindung fehlgeschlagen";

            if (IsConnected)
            {
                var settings = _settingsService.Load();
                settings.ServerUrl = ServerUrl;
                settings.ApiKey = ApiKey;
                settings.OllamaUrl = OllamaUrl;
                _settingsService.Save(settings);
            }
        }
        finally
        {
            IsTesting = false;
        }
    }
}
