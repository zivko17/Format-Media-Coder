using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Profiles;
using FormatMediaCoder.Core.Services;
using Xunit;

namespace FormatMediaCoder.Core.Tests;

/// <summary>
/// Tests del constructor de planes. Verifica la regla de oro del brief §4.3:
/// si un archivo ya cumple, se COPIA, no se recodifica.
/// </summary>
public class PrepPlanBuilderTests
{
    private static TargetProfile ResolumeAlpha() =>
        BuiltinProfiles.All().Single(p => p.Id == "resolume-hap-alpha");

    private static MediaInfo Media(
        string ext = "mov", string container = "mov,mp4,m4a",
        string codec = "hap", string pix = "rgba", int w = 1920, int h = 1080, double fps = 25) =>
        new()
        {
            FilePath = $"C:/clips/logo.{ext}",
            ContainerFormat = container,
            SizeBytes = 5_000_000,
            Video = new VideoStreamInfo { Codec = codec, Width = w, Height = h, Fps = fps, PixelFormat = pix },
        };

    [Fact]
    public void CompliantFile_IsCopied_NotRecoded()
    {
        var report = ComplianceService.Evaluate(Media(), ResolumeAlpha());
        var plan = PrepPlanBuilder.Build(report);

        Assert.Equal(PrepAction.Copy, plan.Action);
        Assert.Null(plan.Encode);
    }

    [Fact]
    public void H264File_IsRecoded_ToHapAlpha_WithCorrectVariant()
    {
        var report = ComplianceService.Evaluate(
            Media(ext: "mp4", container: "mov,mp4,m4a", codec: "h264", pix: "yuv420p"),
            ResolumeAlpha());
        var plan = PrepPlanBuilder.Build(report);

        Assert.Equal(PrepAction.Recode, plan.Action);
        Assert.NotNull(plan.Encode);
        Assert.Equal("hap", plan.Encode!.VideoCodec);
        // La corrección del bug del alfa del brief §5: la variante SÍ se especifica.
        Assert.Equal("hap_alpha", plan.Encode.HapVariant);
        Assert.Equal("mov", plan.Encode.Container);
        Assert.EndsWith(".mov", plan.OutputName);
    }

    [Fact]
    public void IncompleteProfile_ProducesSkip_WithReason()
    {
        var led = BuiltinProfiles.All().Single(p => p.Id == "led-player");
        var report = ComplianceService.Evaluate(Media(codec: "h264"), led);
        var plan = PrepPlanBuilder.Build(report);

        Assert.Equal(PrepAction.Skip, plan.Action);
        Assert.False(string.IsNullOrWhiteSpace(plan.SkipReason));
        Assert.Null(plan.Encode);
    }

    [Fact]
    public void GeometryWarnOnly_StillCopies_DoesNotTouchImage()
    {
        var profile = ResolumeAlpha();
        profile.Geometry.AcceptedResolutions.Add(new Resolution(2048, 512));
        // política Warn: geometría no encaja pero codec/pixel sí

        var report = ComplianceService.Evaluate(Media(codec: "hap", pix: "rgba", w: 1920, h: 1080), profile);
        var plan = PrepPlanBuilder.Build(report);

        // No hay re-encode: se copia y se deja el aviso. No se deforma la imagen.
        Assert.Equal(PrepAction.Copy, plan.Action);
        Assert.Null(plan.Encode);
        Assert.Contains(plan.Steps, s => s.Contains("Aviso"));
    }
}
