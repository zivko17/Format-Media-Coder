namespace FormatMediaCoder.Core.Models;

/// <summary>
/// Tamaño de diapositiva en EMU (English Metric Units): la unidad de OOXML,
/// 914400 por pulgada. Un .pptx tiene UN solo tamaño de diapositiva para todo
/// el archivo, así que esto se decide una vez por presentación.
/// </summary>
public readonly record struct SlideSize(long WidthEmu, long HeightEmu)
{
    public const long EmuPerInch = 914400;
    public const long EmuPerPoint = 12700;   // 914400 / 72

    /// <summary>PowerPoint solo acepta lados de 1 a 56 pulgadas.</summary>
    public const long MinEmu = EmuPerInch;
    public const long MaxEmu = 56 * EmuPerInch;

    /// <summary>16:9 — 13,333 × 7,5 pulgadas. El estándar de PowerPoint moderno.</summary>
    public static SlideSize Widescreen => new(12192000, 6858000);

    /// <summary>4:3 — 10 × 7,5 pulgadas. Proyectores viejos y algunos LED.</summary>
    public static SlideSize Standard => new(9144000, 6858000);

    /// <summary>
    /// Tamaño tomado de la página del PDF (en puntos, 72 por pulgada), recortado
    /// a los límites que admite PowerPoint. Así un A4 apaisado o un formato raro
    /// de LED conserva su proporción exacta en vez de meterse a la fuerza en 16:9.
    /// </summary>
    public static SlideSize FromPoints(double widthPoints, double heightPoints)
    {
        if (widthPoints <= 0 || heightPoints <= 0) return Widescreen;
        return new SlideSize(
            Clamp((long)Math.Round(widthPoints * EmuPerPoint)),
            Clamp((long)Math.Round(heightPoints * EmuPerPoint)));
    }

    private static long Clamp(long emu) => emu < MinEmu ? MinEmu : emu > MaxEmu ? MaxEmu : emu;

    public double AspectRatio => HeightEmu == 0 ? 0 : (double)WidthEmu / HeightEmu;

    /// <summary>Etiqueta legible para la interfaz, p. ej. «13,3 × 7,5 in».</summary>
    public string InchesLabel =>
        $"{WidthEmu / (double)EmuPerInch:0.##} × {HeightEmu / (double)EmuPerInch:0.##} in";
}
