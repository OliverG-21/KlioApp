using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using KlioApp.Core.Interfaces;
using KlioApp.Core.Models;

namespace KlioApp.Client;

public class KlioApiClient : IKlioApiClient
{
    private readonly HttpClient _http;
    private string _baseUrl = string.Empty;
    private string _apiKey = string.Empty;

    public KlioApiClient(HttpClient http)
    {
        _http = http;
    }

    public void Configure(string serverUrl, string apiKey)
    {
        _baseUrl = serverUrl.TrimEnd('/');
        _apiKey = apiKey;
    }

    private string Url(string path) => $"{_baseUrl}/{path}";

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, Url("api/v1/folders"));
            AddAuth(request);
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<FolderTreeResponse> GetFolderTreeAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Url("api/v1/folders"));
        AddAuth(request);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FolderTreeResponse>() ?? new FolderTreeResponse();
    }

    public async Task<List<AlbumInfo>> GetAlbumsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Url("api/v1/albums"));
        AddAuth(request);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AlbumInfo>>() ?? [];
    }

    public async Task<FolderNode> CreateFolderAsync(string name, int? parentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Url("api/v1/folders"));
        AddAuth(request);
        request.Content = JsonContent.Create(new { name, parentId });
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FolderNode>())!;
    }

    public async Task<AlbumInfo> CreateAlbumAsync(string name, int? folderId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Url("api/v1/albums"));
        AddAuth(request);
        request.Content = JsonContent.Create(new { name, folderId });
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AlbumInfo>())!;
    }

    public async Task<bool> CheckDuplicateAsync(string fileHash)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Url("api/v1/images/check-duplicate"));
        AddAuth(request);
        request.Content = JsonContent.Create(new { hash = fileHash });
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return false;
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("isDuplicate").GetBoolean();
    }

    public async Task UploadImageAsync(string filePath, int albumId, List<string> tags, IProgress<int>? progress = null)
    {
        using var content = new MultipartFormDataContent();
        var fileStream = File.OpenRead(filePath);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(filePath));
        content.Add(new StringContent(albumId.ToString()), "albumId");

        if (tags.Count > 0)
        {
            content.Add(new StringContent(string.Join(",", tags)), "tags");
            content.Add(new StringContent("auto-local"), "tagSource");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, Url("api/v1/images/upload"));
        AddAuth(request);
        request.Content = content;

        progress?.Report(10);
        var response = await _http.SendAsync(request);
        progress?.Report(100);
        response.EnsureSuccessStatusCode();
    }

    public static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void AddAuth(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_apiKey))
            request.Headers.Add("X-Api-Key", _apiKey);
    }
}
