namespace KlioApp.Core.Models;

public class FolderTreeResponse
{
    public List<FolderNode> Folders { get; set; } = [];
    public List<AlbumInfo> RootAlbums { get; set; } = [];
}
