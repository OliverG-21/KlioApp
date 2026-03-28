using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using KlioApp.UI.ViewModels;

namespace KlioApp.UI.Views;

public partial class FileBrowserView : UserControl
{
    public FileBrowserView()
    {
        InitializeComponent();
        var btn = this.FindControl<Button>("BrowseButton")!;
        btn.Click += OnBrowseClicked;
    }

    private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Ordner auswählen",
            AllowMultiple = false
        });

        if (folders.Count > 0 && DataContext is FileBrowserViewModel vm)
            await vm.OpenFolderAsync(folders[0].Path.LocalPath);
    }
}
