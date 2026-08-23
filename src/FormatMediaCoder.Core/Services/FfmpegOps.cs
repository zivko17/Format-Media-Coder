using System.Globalization;

namespace FormatMediaCoder.Core.Services;

/// <summary>
/// Construcción de argumentos de FFmpeg para las herramientas del trabajo suelto
/// (convertir, comprimir, audio, frames, imagen, recortar). Lógica PURA y
/// testeable, portada de la versión Electron (electron/main.js) manteniendo su
/// comportamiento, incluida la corrección del alfa HAP.
/// </summary>
public static class FfmpegOps
{
    // ── Convertir vídeo ─────────────────────────────────────────────────────
    public sealed class ConvertOptions
    {
        public string Input { get; init; } = "";
        public string Output { get; init; } = "";
        public string? VideoCodec { get; init; }   // libx264 | h264_nvenc | hevc_nvenc | prores | hap | copy | null
        public string? HapVariant { get; init; }    // hap | hap_alpha | hap_q
        public string? AudioCodec { get; init; }    // aac | libmp3lame | pcm_s16le | copy | null
        public string? VideoBitrate { get; init; }  // ej. "8000k"
        public string? AudioBitrate { get; init; }  // ej. "192k"
        public bool NormalizeAudio { get; init; }    // EBU R128 / -23 LUFS
    }

    public static List<string> BuildConvertArgs(ConvertOptions o)
    {
        var args = new List<string> { "-y", "-i", o.Input };
        var vf = new List<string>();

        switch (o.VideoCodec)
        {
            case null:
                break;
            case "h264_nvenc":
                args.AddRange(new[] { "-c:v", "h264_nvenc", "-preset", "p4", "-rc", "vbr", "-cq", "23" });
                break;
            case "hevc_nvenc":
                args.AddRange(new[] { "-c:v", "hevc_nvenc", "-preset", "p4", "-rc", "vbr", "-cq", "28" });
                break;
            case "prores":
                args.AddRange(new[] { "-c:v", "prores_ks", "-profile:v", "3", "-vendor", "apl0" });
                break;
            case "hap":
            {
                // Corrección del alfa (brief §5): variante explícita con -format.
                var variant = string.IsNullOrEmpty(o.HapVariant) ? "hap" : o.HapVariant;
                vf.Add(variant == "hap_alpha" ? "format=rgba" : "format=rgb24");
                args.Add("-c:v"); args.Add("hap");
                if (variant != "hap") { args.Add("-format"); args.Add(variant); }
                break;
            }
            case "copy":
                args.Add("-c:v"); args.Add("copy");
                break;
            default:
                args.Add("-c:v"); args.Add(o.VideoCodec);
                break;
        }

        if (!string.IsNullOrEmpty(o.AudioCodec))
        {
            args.Add("-c:a"); args.Add(o.AudioCodec);
        }

        // Los formatos intra-frame (ProRes, HAP) se asfixian con bitrate forzado: se evita.
        bool intra = o.VideoCodec is "prores" or "hap" or "copy";
        if (!string.IsNullOrEmpty(o.VideoBitrate) && !intra)
        {
            args.Add("-b:v"); args.Add(o.VideoBitrate);
        }
        if (!string.IsNullOrEmpty(o.AudioBitrate))
        {
            args.Add("-b:a"); args.Add(o.AudioBitrate);
        }

        if (vf.Count > 0)
        {
            args.Add("-vf"); args.Add(string.Join(",", vf));
        }
        if (o.NormalizeAudio)
        {
            args.Add("-af"); args.Add("loudnorm=I=-23:LRA=7:TP=-2.0");
        }

        args.Add(o.Output);
        return args;
    }

    // ── Comprimir (libx264 + CRF) ───────────────────────────────────────────
    public sealed class CompressOptions
    {
        public string Input { get; init; } = "";
        public string Output { get; init; } = "";
        public int Crf { get; init; } = 23;
        public string Preset { get; init; } = "medium";
        public string? Resolution { get; init; } // "1280x720" | null
        public string? AudioBitrate { get; init; }
        public bool RemoveAudio { get; init; }
    }

    public static List<string> BuildCompressArgs(CompressOptions o)
    {
        var args = new List<string>
        {
            "-y", "-i", o.Input,
            "-c:v", "libx264", "-crf", o.Crf.ToString(), "-preset", o.Preset,
        };
        if (!string.IsNullOrEmpty(o.Resolution) && o.Resolution != "original")
        {
            args.Add("-vf"); args.Add($"scale={o.Resolution.Replace("x", ":")}");
        }
        if (o.RemoveAudio) args.Add("-an");
        else if (!string.IsNullOrEmpty(o.AudioBitrate)) { args.Add("-b:a"); args.Add(o.AudioBitrate); }
        args.Add(o.Output);
        return args;
    }

    // ── Extraer audio ───────────────────────────────────────────────────────
    private static readonly Dictionary<string, string> AudioCodecByFormat = new()
    {
        ["mp3"] = "libmp3lame", ["aac"] = "aac", ["flac"] = "flac",
        ["wav"] = "pcm_s16le", ["ogg"] = "libvorbis",
    };

    public static List<string> BuildExtractAudioArgs(string input, string output, string format, string? bitrate)
    {
        var codec = AudioCodecByFormat.TryGetValue(format, out var c) ? c : "libmp3lame";
        var args = new List<string> { "-y", "-i", input, "-vn", "-c:a", codec };
        if (format is not ("wav" or "flac"))
        {
            args.Add("-b:a"); args.Add(string.IsNullOrEmpty(bitrate) ? "192k" : bitrate);
        }
        args.Add(output);
        return args;
    }

    // ── Convertir audio ─────────────────────────────────────────────────────
    public sealed class AudioConvertOptions
    {
        public string Input { get; init; } = "";
        public string Output { get; init; } = "";
        public string Codec { get; init; } = "libmp3lame";
        public string? Bitrate { get; init; }
        public int? SampleRate { get; init; }
        public bool Mono { get; init; }
        public bool Normalize { get; init; }
    }

    public static List<string> BuildAudioConvertArgs(AudioConvertOptions o)
    {
        var args = new List<string> { "-y", "-i", o.Input, "-vn", "-c:a", o.Codec };
        if (!string.IsNullOrEmpty(o.Bitrate)) { args.Add("-b:a"); args.Add(o.Bitrate); }
        if (o.SampleRate is int sr) { args.Add("-ar"); args.Add(sr.ToString()); }
        if (o.Mono) { args.Add("-ac"); args.Add("1"); }
        if (o.Normalize) { args.Add("-af"); args.Add("loudnorm=I=-23:LRA=7:TP=-2.0"); }
        args.Add(o.Output);
        return args;
    }

    // ── Extraer frames ──────────────────────────────────────────────────────
    public static List<string> BuildExtractFramesArgs(string input, string outPattern, double fps, int quality)
    {
        return new List<string>
        {
            "-y", "-i", input,
            "-vf", $"fps={fps.ToString("0.###", CultureInfo.InvariantCulture)}",
            "-q:v", quality.ToString(),
            outPattern,
        };
    }

    // ── Convertir imagen ────────────────────────────────────────────────────
    public static List<string> BuildImageConvertArgs(string input, string output, string format)
    {
        var args = new List<string> { "-y", "-i", input };
        if (format == "ico")
        {
            // Windows agradece iconos cuadrados de 256px.
            args.Add("-vf"); args.Add("scale=256:256");
        }
        args.Add(output);
        return args;
    }

    // ── Recortar (sin recodificar) ──────────────────────────────────────────
    public static List<string> BuildTrimArgs(string input, string output, string start, string end)
    {
        double dur = TimeToSeconds(end) - TimeToSeconds(start);
        return new List<string>
        {
            "-y", "-ss", start, "-i", input,
            "-t", dur.ToString("0.###", CultureInfo.InvariantCulture),
            "-c", "copy", output,
        };
    }

    // ── Unir (concat demuxer, sin recodificar) ──────────────────────────────
    // El servicio escribe el fichero de lista (una línea "file '<ruta>'" por clip);
    // aquí solo se construyen los argumentos.
    public static List<string> BuildConcatArgs(string listPath, string output)
    {
        return new List<string>
        {
            "-y", "-f", "concat", "-safe", "0", "-i", listPath, "-c", "copy", output,
        };
    }

    /// <summary>Línea del fichero de lista del concat demuxer, con escape de comillas.</summary>
    public static string ConcatListLine(string filePath) =>
        "file '" + filePath.Replace("'", "'\\''") + "'";

    // ── Filtros (escala / rotación) ─────────────────────────────────────────
    public static List<string> BuildFiltersArgs(string input, string output, Models.Resolution? scale, string? rotation)
    {
        var args = new List<string> { "-y", "-i", input };
        var vf = new List<string>();
        if (scale is Models.Resolution r) vf.Add($"scale={r.Width}:{r.Height}");
        switch (rotation)
        {
            case "cw": vf.Add("transpose=1"); break;
            case "ccw": vf.Add("transpose=2"); break;
            case "180": vf.Add("hflip"); vf.Add("vflip"); break;
        }
        if (vf.Count > 0) { args.Add("-vf"); args.Add(string.Join(",", vf)); }
        args.Add("-c:a"); args.Add("copy");
        args.Add(output);
        return args;
    }

    public static double TimeToSeconds(string t)
    {
        if (string.IsNullOrEmpty(t)) return 0;
        var parts = t.Split(':').Select(p => double.TryParse(p, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0).ToArray();
        return parts.Length switch
        {
            3 => parts[0] * 3600 + parts[1] * 60 + parts[2],
            2 => parts[0] * 60 + parts[1],
            1 => parts[0],
            _ => 0,
        };
    }
}
