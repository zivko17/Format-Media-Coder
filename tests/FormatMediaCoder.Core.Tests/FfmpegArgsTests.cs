using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Services;
using Xunit;

namespace FormatMediaCoder.Core.Tests;

/// <summary>
/// Tests de la construcción de argumentos de FFmpeg. El caso estrella es la
/// corrección del bug del alfa (brief §5): que la variante HAP se pase con
/// -format y no se quede en la variante por defecto que descarta la transparencia.
/// </summary>
public class FfmpegArgsTests
{
    private static PrepPlan RecodePlan(EncodeSpec spec) => new()
    {
        Media = new MediaInfo { FilePath = "C:/in/clip.mp4" },
        Profile = new TargetProfile { Id = "x", Name = "x" },
        Action = PrepAction.Recode,
        OutputName = "clip.mov",
        Encode = spec,
    };

    [Fact]
    public void HapAlpha_PassesFormatFlag_And_RgbaFilter()
    {
        var args = FfmpegArgs.BuildEncodeArgs(
            RecodePlan(new EncodeSpec { VideoCodec = "hap", HapVariant = "hap_alpha" }),
            "C:/out/clip.mov");

        // El corazón del brief §5: -format hap_alpha DEBE estar presente.
        int fmtIdx = args.IndexOf("-format");
        Assert.True(fmtIdx >= 0, "Falta -format: se perdería el canal alfa.");
        Assert.Equal("hap_alpha", args[fmtIdx + 1]);

        // Y la entrada debe ser rgba para conservar la transparencia.
        int vfIdx = args.IndexOf("-vf");
        Assert.True(vfIdx >= 0);
        Assert.Contains("format=rgba", args[vfIdx + 1]);

        Assert.Contains("hap", args);
    }

    [Fact]
    public void HapQ_PassesFormatFlag_And_RgbFilter()
    {
        var args = FfmpegArgs.BuildEncodeArgs(
            RecodePlan(new EncodeSpec { VideoCodec = "hap", HapVariant = "hap_q" }),
            "C:/out/clip.mov");

        int fmtIdx = args.IndexOf("-format");
        Assert.True(fmtIdx >= 0);
        Assert.Equal("hap_q", args[fmtIdx + 1]);

        int vfIdx = args.IndexOf("-vf");
        Assert.Contains("format=rgb24", args[vfIdx + 1]);
    }

    [Fact]
    public void PlainHap_DoesNotPassFormat_ButStaysDefault()
    {
        // La variante 'hap' es la de por defecto: no hace falta -format, pero SÍ rgb.
        var args = FfmpegArgs.BuildEncodeArgs(
            RecodePlan(new EncodeSpec { VideoCodec = "hap", HapVariant = "hap" }),
            "C:/out/clip.mov");

        Assert.DoesNotContain("-format", args);
        int vfIdx = args.IndexOf("-vf");
        Assert.Contains("format=rgb24", args[vfIdx + 1]);
    }

    [Fact]
    public void Loudnorm_UsesTargetLufs()
    {
        var args = FfmpegArgs.BuildEncodeArgs(
            RecodePlan(new EncodeSpec { VideoCodec = "hap", HapVariant = "hap_q", NormalizeLoudness = -23 }),
            "C:/out/clip.mov");

        int afIdx = args.IndexOf("-af");
        Assert.True(afIdx >= 0);
        Assert.Contains("loudnorm=I=-23", args[afIdx + 1]);
    }

    [Fact]
    public void Scale_EmitsScaleFilter()
    {
        var args = FfmpegArgs.BuildEncodeArgs(
            RecodePlan(new EncodeSpec { VideoCodec = "hap", HapVariant = "hap_q", Scale = new Resolution(2048, 512) }),
            "C:/out/clip.mov");

        int vfIdx = args.IndexOf("-vf");
        Assert.Contains("scale=2048:512", args[vfIdx + 1]);
    }

    [Fact]
    public void OutputPath_IsLastArgument()
    {
        var args = FfmpegArgs.BuildEncodeArgs(
            RecodePlan(new EncodeSpec { VideoCodec = "hap", HapVariant = "hap_alpha" }),
            "C:/out/clip.mov");

        Assert.Equal("C:/out/clip.mov", args[^1]);
    }
}
