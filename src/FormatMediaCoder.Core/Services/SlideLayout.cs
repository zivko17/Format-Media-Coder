using FormatMediaCoder.Core.Models;

namespace FormatMediaCoder.Core.Services;

/// <summary>Cómo se elige el tamaño de la diapositiva al montar el .pptx.</summary>
public enum SlideSizeMode
{
    /// <summary>16:9, lo que espera cualquier proyector de evento.</summary>
    Widescreen16x9,
    /// <summary>4:3, para material y proyectores antiguos.</summary>
    Standard4x3,
    /// <summary>La proporción real de la primera página del PDF.</summary>
    MatchSource,
}

/// <summary>
/// Aritmética de colocación de las páginas dentro de la diapositiva. Lógica PURA:
/// aquí no hay ni PDF ni PowerPoint, solo números. Es lo que garantiza que una
/// página nunca sale deformada (regla de oro del material de evento: si el
/// cliente manda un A4 vertical, se ve entero con bandas, no estirado).
/// </summary>
public static class SlideLayout
{
    public static SlideSize Resolve(SlideSizeMode mode, double sourceWidthPoints, double sourceHeightPoints) => mode switch
    {
        SlideSizeMode.Standard4x3 => SlideSize.Standard,
        SlideSizeMode.MatchSource => SlideSize.FromPoints(sourceWidthPoints, sourceHeightPoints),
        _ => SlideSize.Widescreen,
    };

    /// <summary>Posición y tamaño finales de la imagen dentro de la diapositiva, en EMU.</summary>
    public readonly record struct Placement(long OffsetXEmu, long OffsetYEmu, long WidthEmu, long HeightEmu);

    /// <summary>
    /// Encaja la página dentro de la diapositiva conservando su proporción y
    /// centrándola (contain, nunca recorta ni deforma). Si las proporciones
    /// coinciden, ocupa la diapositiva entera y no hay bandas.
    /// </summary>
    public static Placement Fit(int pixelWidth, int pixelHeight, SlideSize slide)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0)
            return new Placement(0, 0, slide.WidthEmu, slide.HeightEmu);

        double pageAspect = (double)pixelWidth / pixelHeight;

        long width = slide.WidthEmu;
        long height = (long)Math.Round(slide.WidthEmu / pageAspect);

        if (height > slide.HeightEmu)
        {
            height = slide.HeightEmu;
            width = (long)Math.Round(slide.HeightEmu * pageAspect);
        }

        return new Placement((slide.WidthEmu - width) / 2, (slide.HeightEmu - height) / 2, width, height);
    }

    /// <summary>
    /// Píxeles a los que rasterizar una página para que su lado largo mida
    /// <paramref name="longEdgePixels"/>, conservando la proporción. El PDF es
    /// vectorial: la resolución la decidimos nosotros, y de esto depende que el
    /// texto se lea en una pantalla LED de 6 metros.
    /// </summary>
    public static (int Width, int Height) RenderPixels(double pageWidth, double pageHeight, int longEdgePixels)
    {
        if (pageWidth <= 0 || pageHeight <= 0) return (longEdgePixels, longEdgePixels);
        if (longEdgePixels < 1) longEdgePixels = 1;

        double scale = longEdgePixels / Math.Max(pageWidth, pageHeight);
        int w = (int)Math.Round(pageWidth * scale);
        int h = (int)Math.Round(pageHeight * scale);
        return (Math.Max(1, w), Math.Max(1, h));
    }
}
