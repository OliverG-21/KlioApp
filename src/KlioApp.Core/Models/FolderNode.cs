namespace KlioApp.Core.Models;

public class FolderNode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public List<FolderNode> Children { get; set; } = [];
    public List<AlbumInfo> Albums { get; set; } = [];
}
