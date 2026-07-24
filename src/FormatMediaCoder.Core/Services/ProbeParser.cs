using System.Globalization;
using System.Text.Json;
using FormatMediaCoder.Core.Models;

namespace FormatMediaCoder.Core.Services;

/// <summary>
/// Traduce el JSON de <c>ffprobe -print_format json</c> a un <see cref="MediaInfo"/>.
/// Lógica pura y testeable: recibe el texto JSON, no ejecuta el proceso.
/// </summary>
public static class ProbeParser
{
    public static MediaInfo Parse(string filePath, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        VideoStreamInfo? video = null;
        AudioStreamInfo? audio = null;

        if (root.TryGetProperty("streams", out var streams) && streams.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in streams.EnumerateArray())
            {
                var type = Str(s, "codec_type");
                if (type == "video" && video is null)
                {
                    video = new VideoStreamInfo
                    {
                        Codec = Str(s, "codec_name") ?? "",
                        Width = Int(s, "width"),
                        Height = Int(s, "height"),
                        Fps = ParseFraction(Str(s, "r_frame_rate")),
                        PixelFormat = Str(s, "pix_fmt") ?? "",
                    };
                }
                else if (type == "audio" && audio is null)
                {
                    audio = new AudioStreamInfo
                    {
                        Codec = Str(s, "codec_name") ?? "",
                        Channels = Int(s, "channels"),
                        SampleRate = int.TryParse(Str(s, "sample_rate"), out var sr) ? sr : 0,
                    };
                }
            }
        }

        string container = "";
        double duration = 0;
        long size = 0, bitrate = 0;
        if (root.TryGetProperty("format", out var fmt) && fmt.ValueKind == JsonValueKind.Object)
        {
            container = Str(fmt, "format_name") ?? "";
            duration = double.TryParse(Str(fmt, "duration"), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
            size = long.TryParse(Str(fmt, "size"), out var sz) ? sz : 0;
            bitrate = long.TryParse(Str(fmt, "bit_rate"), out var br) ? br : 0;
        }

        return new MediaInfo
        {
            FilePath = filePath,
            ContainerFormat = container,
            DurationSeconds = duration,
            SizeBytes = size,
            BitRate = bitrate,
            Video = video,
            Audio = audio,
        };
    }

    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v)
            ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString())
            : null;

    private static int Int(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : 0;

    private static double ParseFraction(string? frac)
    {
        if (string.IsNullOrEmpty(frac)) return 0;
        var parts = frac.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var n) &&
            double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d != 0)
            return n / d;
        return double.TryParse(frac, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
