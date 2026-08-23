namespace FormatMediaCoder.Core.Services;

/// <summary>
/// Construcción de argumentos para yt-dlp. Lógica pura y testeable. El servicio
/// (App) añade la ruta de FFmpeg (--ffmpeg-location) y ejecuta el proceso.
/// </summary>
public static class YtDlpArgs
{
    public enum Kind
    {
        /// <summary>Mejor vídeo+audio, remuxado a MP4.</summary>
        Video,
        /// <summary>Solo audio, extraído a MP3.</summary>
        AudioMp3,
        /// <summary>Solo audio, mejor pista tal cual.</summary>
        AudioBest,
    }

    public static List<string> Build(string url, string outputDir, Kind kind)
    {
        var outTemplate = System.IO.Path.Combine(outputDir, "%(title)s.%(ext)s");
        var args = new List<string>
        {
            "--no-playlist",
            "--newline",          // progreso línea a línea (parseable)
            "-o", outTemplate,
        };

        switch (kind)
        {
            case Kind.Video:
                args.Add("-f"); args.Add("bv*+ba/b");
                args.Add("--merge-output-format"); args.Add("mp4");
                break;
            case Kind.AudioMp3:
                args.Add("-x");
                args.Add("--audio-format"); args.Add("mp3");
                args.Add("--audio-quality"); args.Add("0");
                break;
            case Kind.AudioBest:
                args.Add("-f"); args.Add("ba/b");
                break;
        }

        args.Add(url);
        return args;
    }
}
