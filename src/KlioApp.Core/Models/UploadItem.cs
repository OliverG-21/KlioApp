namespace KlioApp.Core.Models;

public enum UploadStatus { Pending, Uploading, Done, Failed }

public class UploadItem
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public int TargetAlbumId { get; set; }
    public string TargetAlbumName { get; set; } = string.Empty;
    public UploadStatus Status { get; set; } = UploadStatus.Pending;
    public string? ErrorMessage { get; set; }
    public List<string> Tags { get; set; } = [];
}
