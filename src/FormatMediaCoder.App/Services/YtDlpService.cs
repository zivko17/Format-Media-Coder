using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.Services;

/// <summary>
/// Envoltorio de yt-dlp por proceso: descarga vídeo/audio de una URL, con
/// progreso y cancelación. Usa el FFmpeg incluido en bin\ para remux/extracción.
/// </summary>
public sealed class YtDlpService
{
    private readonly string _ytdlp;
    private readonly string _ffmpegDir;
    private Process? _active;

    public event Action<double, string>? Progress; // (porcentaje 0..100, línea)

    public YtDlpService(string ytdlpPath, string ffmpegDir)
    {
        _ytdlp = ytdlpPath;
        _ffmpegDir = ffmpegDir;
    }

    public static YtDlpService CreateDefault()
    {
        var baseDir = AppContext.BaseDirectory;
        var yt = Path.Combine(baseDir, "bin", "yt-dlp.exe");
        if (!File.Exists(yt)) yt = "yt-dlp";
        return new YtDlpService(yt, Path.Combine(baseDir, "bin"));
    }

    private static readonly Regex PercentRx = new(@"\[download\]\s+([\d.]+)%", RegexOptions.Compiled);

    public async Task DownloadAsync(string url, string outputDir, YtDlpArgs.Kind kind, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);
        var args = YtDlpArgs.Build(url, outputDir, kind).ToList();

        // Apuntar yt-dlp al FFmpeg incluido, si está.
        if (File.Exists(Path.Combine(_ffmpegDir, "ffmpeg.exe")))
        {
            args.Insert(0, _ffmpegDir);
            args.Insert(0, "--ffmpeg-location");
        }

        var psi = new ProcessStartInfo
        {
            FileName = _ytdlp,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = new Process { StartInfo = psi };
        var stderr = new StringBuilder();

        proc.OutputDataReceived += (_, ev) => { if (ev.Data is not null) ReportProgress(ev.Data); };
        proc.ErrorDataReceived += (_, ev) => { if (ev.Data is not null) stderr.AppendLine(ev.Data); };

        proc.Start();
        _active = proc;
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        try { await proc.WaitForExitAsync(ct); }
        catch (OperationCanceledException) { try { proc.Kill(entireProcessTree: true); } catch { } throw; }
        finally { _active = null; }

        if (proc.ExitCode != 0)
        {
            var msg = stderr.ToString().Trim();
            throw new InvalidOperationException(string.IsNullOrEmpty(msg) ? "yt-dlp terminó con error." : msg);
        }
    }

    private void ReportProgress(string line)
    {
        var m = PercentRx.Match(line);
        if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
            Progress?.Invoke(pct, line);
        else
            Progress?.Invoke(-1, line);
    }

    public void Cancel()
    {
        try { _active?.Kill(entireProcessTree: true); } catch { }
    }
}
