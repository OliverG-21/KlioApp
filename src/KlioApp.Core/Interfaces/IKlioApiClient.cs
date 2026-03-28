using KlioApp.Core.Models;

namespace KlioApp.Core.Interfaces;

public interface IKlioApiClient
{
    void Configure(string serverUrl, string apiKey);
    Task<bool> TestConnectionAsync();
    Task<FolderTreeResponse> GetFolderTreeAsync();
    Task<List<AlbumInfo>> GetAlbumsAsync();
    Task<FolderNode> CreateFolderAsync(string name, int? parentId);
    Task<AlbumInfo> CreateAlbumAsync(string name, int? folderId);
    Task<bool> CheckDuplicateAsync(string fileHash);
    Task UploadImageAsync(string filePath, int albumId, List<string> tags, IProgress<int>? progress = null);
}
