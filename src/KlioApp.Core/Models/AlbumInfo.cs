namespace KlioApp.Core.Models;

public class AlbumInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? FolderId { get; set; }
    public int ImageCount { get; set; }
}
