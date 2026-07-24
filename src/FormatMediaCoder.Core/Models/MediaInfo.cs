namespace FormatMediaCoder.Core.Models;

/// <summary>
/// Retrato de un archivo ya analizado. Es lo que el <c>FFmpegService.Probe</c>
/// (envoltorio de <c>ffprobe</c>) entrega al motor de cumplimiento.
/// Es un DTO puro: no toca disco ni FFmpeg. Se puede construir a mano en un test.
/// </summary>
public sealed class MediaInfo
{
    public string FilePath { get; init; } = "";

    /// <summary>Nombre de archivo sin ruta (para nomenclatura y mensajes).</summary>
    public string FileName => System.IO.Path.GetFileName(FilePath);

    /// <summary>Extensión en minúsculas sin punto (ej. "mov", "mp4").</summary>
    public string Extension =>
        System.IO.Path.GetExtension(FilePath).TrimStart('.').ToLowerInvariant();

    /// <summary>Lista de contenedores que declara ffprobe (format_name), ej. "mov,mp4,m4a".</summary>
    public string ContainerFormat { get; init; } = "";

    public double DurationSeconds { get; init; }
    public long SizeBytes { get; init; }
    public long BitRate { get; init; }

    public VideoStreamInfo? Video { get; init; }
    public AudioStreamInfo? Audio { get; init; }

    public bool HasVideo => Video is not null;
    public bool HasAudio => Audio is not null;

    /// <summary>¿El vídeo trae canal alfa? Detonante del problema HAP Alpha del brief §5.</summary>
    public bool HasAlpha => Video?.HasAlpha ?? false;
}

public sealed class VideoStreamInfo
{
    public string Codec { get; init; } = "";
    public int Width { get; init; }
    public int Height { get; init; }

    /// <summary>Fotogramas por segundo ya resueltos (ej. 25, 29.97, 60).</summary>
    public double Fps { get; init; }

    public string PixelFormat { get; init; } = "";

    /// <summary>
    /// Se deduce del pixel format. Los formatos con alfa de FFmpeg lo llevan
    /// en el nombre: rgba, argb, abgr, bgra, yuva420p, yuva444p10le, etc.
    /// </summary>
    public bool HasAlpha =>
        PixelFormat.Contains('a') &&
        (PixelFormat.StartsWith("rgba") || PixelFormat.StartsWith("argb") ||
         PixelFormat.StartsWith("bgra") || PixelFormat.StartsWith("abgr") ||
         PixelFormat.Contains("yuva") || PixelFormat.Contains("ya8") ||
         PixelFormat.Contains("gbra"));

    public string ResolutionLabel => $"{Width}×{Height}";
}

public sealed class AudioStreamInfo
{
    public string Codec { get; init; } = "";
    public int Channels { get; init; }
    public int SampleRate { get; init; }
}
