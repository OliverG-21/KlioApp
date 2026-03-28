namespace KlioApp.Core.Interfaces;

public interface IOllamaClient
{
    void Configure(string ollamaUrl, string model, string prompt);
    Task<bool> IsAvailableAsync();
    Task<List<string>> TagImageAsync(string imagePath, CancellationToken ct = default);
}
