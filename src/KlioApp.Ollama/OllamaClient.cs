using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KlioApp.Core.Interfaces;

namespace KlioApp.Ollama;

public class OllamaClient : IOllamaClient
{
    private readonly HttpClient _http;
    private string _baseUrl = "http://localhost:11434";
    private string _model = "llava-phi3";
    private string _prompt =
        "List up to 8 short descriptive tags for this image in English. " +
        "Use simple general terms for people, places, objects, activities or moods. " +
        "Reply only with a JSON array, no explanation. Example: [\"person\",\"outdoors\",\"summer\"]";

    public OllamaClient(HttpClient http) => _http = http;

    public void Configure(string ollamaUrl, string model, string prompt)
    {
        if (!string.IsNullOrWhiteSpace(ollamaUrl)) _baseUrl = ollamaUrl.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(model)) _model = model;
        if (!string.IsNullOrWhiteSpace(prompt)) _prompt = prompt;
    }

    private string Url(string path) => $"{_baseUrl}/{path}";

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _http.GetAsync(Url("api/tags"));
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<string>> TagImageAsync(string imagePath, CancellationToken ct = default)
    {
        var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
        var base64 = Convert.ToBase64String(imageBytes);

        var payload = new
        {
            model = _model,
            prompt = _prompt,
            images = new[] { base64 },
            stream = false
        };

        var response = await _http.PostAsJsonAsync(Url("api/generate"), payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: ct);
        return ParseTags(result?.Response ?? string.Empty);
    }

    private static List<string> ParseTags(string responseText)
    {
        // Try to extract JSON array from response
        var start = responseText.IndexOf('[');
        var end = responseText.LastIndexOf(']');
        if (start >= 0 && end > start)
        {
            try
            {
                var json = responseText[start..(end + 1)];
                var tags = JsonSerializer.Deserialize<List<string>>(json);
                if (tags != null)
                    return tags
                        .Select(t => t.Trim().ToLowerInvariant())
                        .Where(t => t.Length > 0 && t.Length <= 50)
                        .Distinct()
                        .ToList();
            }
            catch { /* fall through to comma split */ }
        }

        // Fallback: split by comma
        return responseText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim('"', '[', ']', ' ').ToLowerInvariant())
            .Where(t => t.Length > 0 && t.Length <= 50)
            .Distinct()
            .ToList();
    }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
