using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using KlioApp.Client;
using KlioApp.Core.Interfaces;
using KlioApp.Ollama;
using KlioApp.UI.ViewModels;
using KlioApp.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Runtime.InteropServices;

namespace KlioApp.UI;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddHttpClient<KlioApiClient>();
        services.AddSingleton<IKlioApiClient>(sp => sp.GetRequiredService<KlioApiClient>());
        services.AddSingleton<ISettingsService, SettingsService>();

        services.AddHttpClient<OllamaClient>();
        services.AddSingleton<IOllamaClient>(sp => sp.GetRequiredService<OllamaClient>());

        services.AddTransient<ConnectionViewModel>();
        services.AddTransient<AlbumTreeViewModel>();
        services.AddTransient<FileBrowserViewModel>();
        services.AddTransient<UploadViewModel>();
        services.AddTransient<OllamaTaggingViewModel>();
        services.AddTransient<MainWindowViewModel>();

        _services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            var vm = _services.GetRequiredService<MainWindowViewModel>();

            var settingsService = _services.GetRequiredService<ISettingsService>();
            var apiClient = _services.GetRequiredService<IKlioApiClient>();
            var settings = settingsService.Load();
            if (!string.IsNullOrEmpty(settings.ServerUrl))
                apiClient.Configure(settings.ServerUrl, settings.ApiKey);

            var ollamaClient = _services.GetRequiredService<IOllamaClient>();
            ollamaClient.Configure(settings.OllamaUrl, settings.OllamaModel, string.Empty);

            var iconStream = AssetLoader.Open(new Uri("avares://KlioApp.UI/Assets/klio-icon.png"));
            var appIcon = new WindowIcon(iconStream);

            desktop.MainWindow = new MainWindow { DataContext = vm, Icon = appIcon };

            if (OperatingSystem.IsMacOS())
                SetMacOSDockIcon();

            if (!string.IsNullOrEmpty(settings.ServerUrl))
                _ = vm.AlbumTreeVM.LoadAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void SetMacOSDockIcon()
    {
        try
        {
            // Write embedded PNG to temp file
            var tempPath = Path.Combine(Path.GetTempPath(), "klio-dock-icon.png");
            using (var src = AssetLoader.Open(new Uri("avares://KlioApp.UI/Assets/klio-icon.png")))
            using (var dst = File.Create(tempPath))
                src.CopyTo(dst);

            // Convert C string to NSString* (stringWithUTF8String: takes const char*)
            var nsPath = ObjC.MsgSendStr(
                ObjC.GetClass("NSString"),
                ObjC.Sel("stringWithUTF8String:"),
                tempPath);

            // [[NSImage alloc] initWithContentsOfFile: nsPath]  — needs NSString*
            var nsImage = ObjC.MsgSendPtr(
                ObjC.MsgSend(ObjC.GetClass("NSImage"), ObjC.Sel("alloc")),
                ObjC.Sel("initWithContentsOfFile:"),
                nsPath);

            // [NSApp setApplicationIconImage: nsImage]
            ObjC.MsgSendPtr(
                ObjC.MsgSend(ObjC.GetClass("NSApplication"), ObjC.Sel("sharedApplication")),
                ObjC.Sel("setApplicationIconImage:"),
                nsImage);
        }
        catch
        {
            // Non-fatal
        }
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var toRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
        foreach (var plugin in toRemove)
            BindingPlugins.DataValidators.Remove(plugin);
    }
}

internal static class ObjC
{
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
    public static extern IntPtr GetClass(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
    public static extern IntPtr Sel(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend(IntPtr self, IntPtr sel);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSendPtr(IntPtr self, IntPtr sel, IntPtr arg);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSendStr(IntPtr self, IntPtr sel, string arg);
}
