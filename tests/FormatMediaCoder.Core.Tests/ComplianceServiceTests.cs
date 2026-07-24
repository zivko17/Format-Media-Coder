using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Profiles;
using FormatMediaCoder.Core.Services;
using Xunit;

namespace FormatMediaCoder.Core.Tests;

/// <summary>
/// Tests del motor de cumplimiento. Es la pieza que justifica el producto y la
/// única de lógica pura, así que es la que más merece tests (brief §4.2, §8.4).
/// Todo se prueba con MediaInfo construido a mano: ni disco ni FFmpeg.
/// </summary>
public class ComplianceServiceTests
{
    private static TargetProfile ResolumeAlpha() =>
        BuiltinProfiles.All().Single(p => p.Id == "resolume-hap-alpha");

    private static TargetProfile ResolumeQ() =>
        BuiltinProfiles.All().Single(p => p.Id == "resolume-hap-q");

    private static MediaInfo Media(
        string ext = "mov", string container = "mov,mp4,m4a",
        string codec = "hap", int w = 1920, int h = 1080, double fps = 25,
        string pix = "rgba", bool audio = false, string audioCodec = "pcm_s16le") =>
        new()
        {
            FilePath = $"C:/clips/clip.{ext}",
            ContainerFormat = container,
            SizeBytes = 10_000_000,
            Video = new VideoStreamInfo { Codec = codec, Width = w, Height = h, Fps = fps, PixelFormat = pix },
            Audio = audio ? new AudioStreamInfo { Codec = audioCodec, Channels = 2, SampleRate = 48000 } : null,
        };

    [Fact]
    public void HapAlphaFile_Complies_Green()
    {
        var report = ComplianceService.Evaluate(Media(codec: "hap", pix: "rgba"), ResolumeAlpha());
        Assert.Equal(ComplianceStatus.Ok, report.Status);
        Assert.Empty(report.Deviations);
    }

    [Fact]
    public void H264File_BlocksOn_ResolumeHap()
    {
        var report = ComplianceService.Evaluate(
            Media(ext: "mp4", container: "mov,mp4,m4a", codec: "h264", pix: "yuv420p"),
            ResolumeAlpha());

        Assert.Equal(ComplianceStatus.Block, report.Status);
        Assert.Contains(report.Deviations, d => d.Field == DeviationField.Codec && d.Severity == Severity.Blocks);
        // El codec es auto-reparable (recodificar a HAP).
        Assert.Contains(report.Deviations, d => d.Field == DeviationField.Codec && d.AutoFixable);
    }

    [Fact]
    public void WrongContainer_Blocks()
    {
        // HAP dentro de un .mkv: codec ok, contenedor no.
        var report = ComplianceService.Evaluate(
            Media(ext: "mkv", container: "matroska,webm", codec: "hap", pix: "rgba"),
            ResolumeAlpha());

        Assert.Contains(report.Deviations, d => d.Field == DeviationField.Container && d.Severity == Severity.Blocks);
    }

    [Fact]
    public void AlphaWanted_ButSourceHasNoAlpha_Warns()
    {
        // Codec ya es HAP, pero pixel format sin alfa contra un perfil HAP Alpha.
        var report = ComplianceService.Evaluate(Media(codec: "hap", pix: "rgb24"), ResolumeAlpha());
        Assert.Contains(report.Deviations, d => d.Field == DeviationField.PixelFormat && d.Severity == Severity.Warns);
    }

    [Fact]
    public void AlphaPresent_ButProfileDiscardsIt_Warns()
    {
        // Archivo con alfa contra un perfil HAP Q (sin alfa): aviso de que se pierde la transparencia.
        var report = ComplianceService.Evaluate(Media(codec: "hap", pix: "rgba"), ResolumeQ());
        Assert.Contains(report.Deviations, d => d.Field == DeviationField.PixelFormat && d.Severity == Severity.Warns);
    }

    [Fact]
    public void IncompleteProfile_ReturnsUnknown_NeverGuesses()
    {
        var led = BuiltinProfiles.All().Single(p => p.Id == "led-player");
        var report = ComplianceService.Evaluate(Media(codec: "h264"), led);

        Assert.Equal(ComplianceStatus.Unknown, report.Status);
        Assert.Empty(report.Deviations);
        Assert.False(string.IsNullOrWhiteSpace(report.UnknownReason));
    }

    [Fact]
    public void ResolutionMismatch_WithWarnPolicy_Warns_NotAutoFixable()
    {
        var profile = ResolumeQ();
        profile.Geometry.AcceptedResolutions.Add(new Resolution(2048, 512));
        // política por defecto = Warn ⇒ avisa y NO toca (brief §9c)

        var report = ComplianceService.Evaluate(Media(codec: "hap", pix: "rgb24", w: 1920, h: 1080), profile);

        var dev = Assert.Single(report.Deviations, d => d.Field == DeviationField.Resolution);
        Assert.Equal(Severity.Warns, dev.Severity);
        Assert.False(dev.AutoFixable); // no se toca la imagen automáticamente
    }

    [Fact]
    public void ResolutionMismatch_WithPadPolicy_IsAutoFixable()
    {
        var profile = ResolumeQ();
        profile.Geometry.AcceptedResolutions.Add(new Resolution(2048, 512));
        profile.Geometry.Policy = GeometryPolicy.Pad;

        var report = ComplianceService.Evaluate(Media(codec: "hap", pix: "rgb24", w: 1920, h: 1080), profile);

        var dev = Assert.Single(report.Deviations, d => d.Field == DeviationField.Resolution);
        Assert.True(dev.AutoFixable);
    }

    [Theory]
    [InlineData(29.97, 30, true)]   // tolerancia broadcast
    [InlineData(23.976, 24, true)]
    [InlineData(25, 30, false)]     // fuera de tolerancia ⇒ avisa
    public void FpsTolerance_Works(double sourceFps, double acceptedFps, bool shouldComply)
    {
        var profile = ResolumeQ();
        profile.Temporal.AcceptedFps.Add(acceptedFps);

        var report = ComplianceService.Evaluate(Media(codec: "hap", pix: "rgb24", fps: sourceFps), profile);
        bool hasFpsDeviation = report.Deviations.Any(d => d.Field == DeviationField.Fps);

        Assert.Equal(shouldComply, !hasFpsDeviation);
    }

    [Fact]
    public void MaxResolutionLimit_Blocks()
    {
        var profile = ResolumeQ();
        profile.Limits.MaxWidth = 4096;
        profile.Limits.MaxHeight = 2160;

        var report = ComplianceService.Evaluate(Media(codec: "hap", pix: "rgb24", w: 7680, h: 4320), profile);
        Assert.Contains(report.Deviations, d => d.Field == DeviationField.Limits && d.Severity == Severity.Blocks);
    }
}
