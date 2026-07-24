using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.Services;

/// <summary>
/// Envoltorio de FFmpeg/ffprobe por proceso. Es el equivalente en .NET del
/// FFmpegService que el brief da por reutilizable (§4.4): probe, ejecución del
/// plan, cancelación, borrado de parciales y extracción del error real.
///
/// La lógica pura (parseo de ffprobe y construcción de argumentos, incluida la
/// corrección del bug del alfa del brief §5) vive en el Core y está cubierta por
/// tests. Aquí solo queda el manejo de proceso, propio de Windows.
/// </summary>
public sealed class FFmpegService
{
    private readonly string _ffmpeg;
    private readonly string _ffprobe;
    private Process? _active;

    public event Action<double, string>? Progress; // (porcentaje 0..100, timemark)

    public FFmpegService(string ffmpegPath, string ffprobePath)
    {
        _ffmpeg = ffmpegPath;
        _ffprobe = ffprobePath;
    }

    /// <summary>Localiza los binarios: bin\ junto al ejecutable primero, PATH después.</summary>
    public static FFmpegService CreateDefault()
    {
        var baseDir = AppContext.BaseDirectory;
        string ff = Path.Combine(baseDir, "bin", "ffmpeg.exe");
        string fp = Path.Combine(baseDir, "bin", "ffprobe.exe");
        if (!File.Exists(ff)) ff = "ffmpeg";
        if (!File.Exists(fp)) fp = "ffprobe";
        return new FFmpegService(ff, fp);
    }

    // ── Probe ──────────────────────────────────────────────────────────────
    public async Task<MediaInfo> ProbeAsync(string filePath, CancellationToken ct = default)
    {
        var args = new[]
        {
            "-v", "quiet", "-print_format", "json",
            "-show_format", "-show_streams", filePath,
        };

        var (exit, stdout, stderr) = await RunCaptureAsync(_ffprobe, args, ct);
        if (exit != 0)
            throw new InvalidOperationException($"ffprobe falló: {stderr}");

        return ProbeParser.Parse(filePath, stdout);
    }

    // ── Ejecución del plan ─────────────────────────────────────────────────
    public async Task ExecuteAsync(PrepPlan plan, string outputPath, CancellationToken ct = default)
    {
        if (plan.Action == PrepAction.Skip)
            throw new InvalidOperationException($"El plan está marcado como no ejecutable: {plan.SkipReason}");

        if (plan.Action is PrepAction.Copy or PrepAction.Rename)
        {
            // Ya cumple: copiar tal cual, sin recodificar (brief §4.3).
            File.Copy(plan.Media.FilePath, outputPath, overwrite: false);
            Progress?.Invoke(100, "");
            return;
        }

        var args = FfmpegArgs.BuildEncodeArgs(plan, outputPath);
        var (exit, _, stderr) = await RunCaptureAsync(_ffmpeg, args, ct, isEncode: true, plan.Media.DurationSeconds);

        if (exit != 0)
        {
            // Nunca dejar parciales (brief §4.4).
            TryDelete(outputPath);
            throw new InvalidOperationException(ExtractRealError(stderr));
        }
    }

    public void Cancel()
    {
        try { _active?.Kill(entireProcessTree: true); } catch { /* ya terminó */ }
    }

    // ── Infraestructura de proceso ─────────────────────────────────────────
    private async Task<(int exit, string stdout, string stderr)> RunCaptureAsync(
        string exe, IReadOnlyList<string> args, CancellationToken ct,
        bool isEncode = false, double durationSeconds = 0)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        proc.OutputDataReceived += (_, ev) => { if (ev.Data != null) stdout.AppendLine(ev.Data); };
        proc.ErrorDataReceived += (_, ev) =>
        {
            if (ev.Data == null) return;
            stderr.AppendLine(ev.Data);
            if (isEncode && durationSeconds > 0) ReportProgress(ev.Data, durationSeconds);
        };

        proc.Start();
        _active = proc;
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        try
        {
            await proc.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw;
        }
        finally
        {
            _active = null;
        }

        return (proc.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private void ReportProgress(string line, double durationSeconds)
    {
        // FFmpeg escribe "time=00:00:12.34" en stderr. Lo traducimos a porcentaje.
        int idx = line.IndexOf("time=", StringComparison.Ordinal);
        if (idx < 0) return;
        var tm = line.Substring(idx + 5).Split(' ')[0].Trim();
        double secs = TimemarkToSeconds(tm);
        if (secs <= 0) return;
        double pct = Math.Clamp(secs / durationSeconds * 100.0, 0, 100);
        Progress?.Invoke(pct, tm);
    }

    private static double TimemarkToSeconds(string tm)
    {
        var parts = tm.Split(':');
        if (parts.Length != 3) return 0;
        return double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var h)
            && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var m)
            && double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var s)
            ? h * 3600 + m * 60 + s : 0;
    }

    /// <summary>De todo el stderr de FFmpeg, la última línea suele ser el error real.</summary>
    private static string ExtractRealError(string stderr)
    {
        var lines = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            var l = lines[i];
            if (l.Length > 0 && !l.StartsWith("frame=") && !l.StartsWith("size=") && !l.StartsWith("video:"))
                return l;
        }
        return "FFmpeg terminó con error.";
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
