using System.IO;

namespace FormatMediaCoder.App.Services;

/// <summary>
/// Nombres de salida únicos que nunca sobrescriben (brief §4.4 y regla §6.3:
/// "nunca sobrescribir archivos en silencio"). Si el destino existe, añade
/// " (2)", " (3)"… antes de la extensión.
/// </summary>
public static class PathUtils
{
    public static string UniquePath(string desiredPath)
    {
        if (!File.Exists(desiredPath)) return desiredPath;

        var dir = Path.GetDirectoryName(desiredPath) ?? "";
        var stem = Path.GetFileNameWithoutExtension(desiredPath);
        var ext = Path.GetExtension(desiredPath);

        for (int i = 2; ; i++)
        {
            var candidate = Path.Combine(dir, $"{stem} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
    }
}
