using Windows.Storage.Pickers;

namespace FormatMediaCoder.App.Views;

/// <summary>Ayudantes de selección de archivos/carpeta con el interop de ventana de WinUI.</summary>
public static class Pickers
{
    public static async Task<string?> PickFileAsync(params string[] extensions)
    {
        var p = new FileOpenPicker();
        if (extensions.Length == 0) p.FileTypeFilter.Add("*");
        else foreach (var e in extensions) p.FileTypeFilter.Add(e);
        WinRT.Interop.InitializeWithWindow.Initialize(p, MainWindow.Hwnd);
        var f = await p.PickSingleFileAsync();
        return f?.Path;
    }

    public static async Task<IReadOnlyList<string>> PickFilesAsync(params string[] extensions)
    {
        var p = new FileOpenPicker();
        if (extensions.Length == 0) p.FileTypeFilter.Add("*");
        else foreach (var e in extensions) p.FileTypeFilter.Add(e);
        WinRT.Interop.InitializeWithWindow.Initialize(p, MainWindow.Hwnd);
        var files = await p.PickMultipleFilesAsync();
        return files.Select(f => f.Path).ToList();
    }

    public static async Task<string?> PickFolderAsync()
    {
        var p = new FolderPicker();
        p.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(p, MainWindow.Hwnd);
        var f = await p.PickSingleFolderAsync();
        return f?.Path;
    }
}
