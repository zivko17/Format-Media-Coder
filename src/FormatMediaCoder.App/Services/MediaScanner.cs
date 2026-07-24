using System.IO;

namespace FormatMediaCoder.App.Services;

/// <summary>
/// Recorre una carpeta (con subcarpetas) buscando material audiovisual. Es el
/// PASO 1 del flujo: "suelta una carpeta entera, como llega del cliente" (brief §6.1).
/// </summary>
public static class MediaScanner
{
    // Extensiones de vídeo/imagen habituales en material de eventos.
    private static readonly HashSet<string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mov", ".mp4", ".m4v", ".mkv", ".avi", ".webm", ".wmv", ".mpg", ".mpeg",
        ".mxf", ".prores", ".hap", ".ts", ".flv", ".gif", ".png", ".jpg", ".jpeg", ".tif", ".tiff",
    };

    public static IReadOnlyList<string> Scan(string folder, bool recurse = true)
    {
        if (!Directory.Exists(folder)) return Array.Empty<string>();
        var opt = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(folder, "*.*", opt)
            .Where(f => MediaExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
