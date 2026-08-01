using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Services;
using Xunit;

namespace FormatMediaCoder.Core.Tests;

/// <summary>Colocación de páginas de PDF dentro de la diapositiva: nunca deformar.</summary>
public class SlideLayoutTests
{
    [Fact]
    public void Fit_SameAspect_FillsSlideWithoutBands()
    {
        var p = SlideLayout.Fit(1920, 1080, SlideSize.Widescreen);

        Assert.Equal(0, p.OffsetXEmu);
        Assert.Equal(0, p.OffsetYEmu);
        Assert.Equal(SlideSize.Widescreen.WidthEmu, p.WidthEmu);
        Assert.Equal(SlideSize.Widescreen.HeightEmu, p.HeightEmu);
    }

    [Fact]
    public void Fit_PortraitPageOnWidescreen_KeepsAspectAndCenters()
    {
        // A4 vertical (595 × 842 pt) rasterizado: cabe en alto y deja bandas a los lados.
        var p = SlideLayout.Fit(1487, 2105, SlideSize.Widescreen);

        Assert.Equal(SlideSize.Widescreen.HeightEmu, p.HeightEmu);          // limita el alto
        Assert.True(p.WidthEmu < SlideSize.Widescreen.WidthEmu);            // sobra ancho
        Assert.True(p.OffsetXEmu > 0);                                       // centrada
        Assert.Equal(0, p.OffsetYEmu);
        Assert.Equal(1487d / 2105d, (double)p.WidthEmu / p.HeightEmu, 3);    // proporción intacta
    }

    [Fact]
    public void Fit_UltraWidePage_LimitsWidthAndLetterboxes()
    {
        var p = SlideLayout.Fit(4000, 1000, SlideSize.Widescreen);

        Assert.Equal(SlideSize.Widescreen.WidthEmu, p.WidthEmu);
        Assert.True(p.HeightEmu < SlideSize.Widescreen.HeightEmu);
        Assert.True(p.OffsetYEmu > 0);
        Assert.Equal(0, p.OffsetXEmu);
    }

    [Fact]
    public void Fit_DegenerateImage_DoesNotDivideByZero()
    {
        var p = SlideLayout.Fit(0, 0, SlideSize.Standard);
        Assert.Equal(SlideSize.Standard.WidthEmu, p.WidthEmu);
        Assert.Equal(SlideSize.Standard.HeightEmu, p.HeightEmu);
    }

    [Fact]
    public void RenderPixels_ScalesLongEdgeAndKeepsAspect()
    {
        var (w, h) = SlideLayout.RenderPixels(595, 842, 1920);   // A4 vertical

        Assert.Equal(1920, h);                                    // el lado largo es el alto
        Assert.Equal(1357, w);
        Assert.Equal(595d / 842d, (double)w / h, 2);
    }

    [Fact]
    public void RenderPixels_NeverReturnsZero()
    {
        var (w, h) = SlideLayout.RenderPixels(1000, 1, 100);
        Assert.True(w >= 1 && h >= 1);
    }

    [Fact]
    public void Resolve_MatchSource_UsesPdfPageProportion()
    {
        var size = SlideLayout.Resolve(SlideSizeMode.MatchSource, 842, 595);  // A4 apaisado

        Assert.Equal(842 * SlideSize.EmuPerPoint, size.WidthEmu);
        Assert.Equal(595 * SlideSize.EmuPerPoint, size.HeightEmu);
    }

    [Fact]
    public void Resolve_MatchSource_ClampsToPowerPointLimits()
    {
        // 100 pulgadas de ancho: PowerPoint no pasa de 56.
        var size = SlideLayout.Resolve(SlideSizeMode.MatchSource, 7200, 72);

        Assert.Equal(SlideSize.MaxEmu, size.WidthEmu);
        Assert.Equal(SlideSize.MinEmu, size.HeightEmu);
    }

    [Fact]
    public void Resolve_Presets_AreTheStandardSizes()
    {
        Assert.Equal(new SlideSize(12192000, 6858000), SlideLayout.Resolve(SlideSizeMode.Widescreen16x9, 0, 0));
        Assert.Equal(new SlideSize(9144000, 6858000), SlideLayout.Resolve(SlideSizeMode.Standard4x3, 0, 0));
    }
}
