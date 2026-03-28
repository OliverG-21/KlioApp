using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KlioApp.Core.Interfaces;
using KlioApp.Core.Models;

namespace KlioApp.UI.ViewModels;

public partial class AlbumTreeNodeViewModel : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isExpanded = true;
    public bool IsFolder { get; init; }
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public ObservableCollection<AlbumTreeNodeViewModel> Children { get; } = [];
}

public partial class AlbumTreeViewModel : ViewModelBase
{
    private readonly IKlioApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<AlbumTreeNodeViewModel> _rootNodes = [];
    [ObservableProperty] private AlbumTreeNodeViewModel? _selectedNode;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _newItemName = string.Empty;

    public AlbumTreeViewModel(IKlioApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var tree = await _apiClient.GetFolderTreeAsync();
            var result = BuildTree(tree.Folders);

            foreach (var album in tree.RootAlbums)
                result.Add(new AlbumTreeNodeViewModel { Id = album.Id, Name = album.Name, IsFolder = false });

            RootNodes = result;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Fehler beim Laden: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CreateFolderAsync()
    {
        if (string.IsNullOrWhiteSpace(NewItemName)) return;
        int? parentId = SelectedNode?.IsFolder == true ? SelectedNode.Id : null;
        var folder = await _apiClient.CreateFolderAsync(NewItemName, parentId);
        var node = new AlbumTreeNodeViewModel { Id = folder.Id, Name = folder.Name, IsFolder = true, ParentId = parentId };
        var parent = parentId.HasValue ? FindNode(RootNodes, parentId.Value, isFolder: true) : null;
        if (parent != null) parent.Children.Add(node);
        else RootNodes.Add(node);
        NewItemName = string.Empty;
    }

    [RelayCommand]
    public async Task CreateAlbumAsync()
    {
        if (string.IsNullOrWhiteSpace(NewItemName)) return;
        int? folderId = SelectedNode?.IsFolder == true ? SelectedNode.Id : null;
        var album = await _apiClient.CreateAlbumAsync(NewItemName, folderId);
        var node = new AlbumTreeNodeViewModel { Id = album.Id, Name = album.Name, IsFolder = false, ParentId = folderId };
        var parent = folderId.HasValue ? FindNode(RootNodes, folderId.Value, isFolder: true) : null;
        if (parent != null) parent.Children.Add(node);
        else RootNodes.Add(node);
        NewItemName = string.Empty;
    }

    public AlbumInfo? GetSelectedAlbum()
    {
        if (SelectedNode?.IsFolder == false)
            return new AlbumInfo { Id = SelectedNode.Id, Name = SelectedNode.Name };
        return null;
    }

    private static ObservableCollection<AlbumTreeNodeViewModel> BuildTree(List<FolderNode> folders)
    {
        var result = new ObservableCollection<AlbumTreeNodeViewModel>();
        foreach (var folder in folders)
            result.Add(BuildFolderNode(folder));
        return result;
    }

    private static AlbumTreeNodeViewModel BuildFolderNode(FolderNode folder)
    {
        var node = new AlbumTreeNodeViewModel { Id = folder.Id, Name = folder.Name, IsFolder = true };
        foreach (var child in folder.Children)
            node.Children.Add(BuildFolderNode(child));
        foreach (var album in folder.Albums)
            node.Children.Add(new AlbumTreeNodeViewModel { Id = album.Id, Name = album.Name, IsFolder = false });
        return node;
    }

    private static AlbumTreeNodeViewModel? FindNode(ObservableCollection<AlbumTreeNodeViewModel> nodes, int id, bool isFolder)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id && node.IsFolder == isFolder) return node;
            var found = FindNode(node.Children, id, isFolder);
            if (found != null) return found;
        }
        return null;
    }
}
