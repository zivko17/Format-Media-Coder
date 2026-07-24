namespace FormatMediaCoder.Core.Models;

/// <summary>Qué se va a hacer con un archivo de entrada.</summary>
public enum PrepAction
{
    /// <summary>Ya cumple: se copia tal cual. NO se recodifica (brief §4.3: recodificar por costumbre pierde calidad y tiempo).</summary>
    Copy,
    /// <summary>Recodificar (codec/variante/escala/fps/audio).</summary>
    Recode,
    /// <summary>Solo renombrar (única desviación cosmética).</summary>
    Rename,
    /// <summary>No se puede decidir (perfil incompleto o bloqueo sin arreglo automático).</summary>
    Skip,
}

/// <summary>
/// Ajustes concretos de codificación que consumirá el FFmpegService.
/// null en un campo = no tocar esa dimensión.
/// </summary>
public sealed class EncodeSpec
{
    public string? VideoCodec { get; init; }
    /// <summary>Variante HAP: "hap" | "hap_alpha" | "hap_q". Corrige el bug del alfa (brief §5).</summary>
    public string? HapVariant { get; init; }
    public string? PixelFormat { get; init; }
    public string? Container { get; init; }

    /// <summary>Escalado destino, o null si la política es no tocar la geometría.</summary>
    public Resolution? Scale { get; init; }
    public double? Fps { get; init; }

    public string? AudioCodec { get; init; }
    public int? AudioChannels { get; init; }
    public int? AudioSampleRate { get; init; }
    /// <summary>LUFS objetivo para loudnorm, o null.</summary>
    public double? NormalizeLoudness { get; init; }
}

/// <summary>
/// Plan de preparación de UN archivo. Se muestra ANTES de ejecutar para que el
/// usuario vea exactamente qué va a pasar y pueda desmarcar lo que no quiera
/// (brief §4.3).
/// </summary>
public sealed class PrepPlan
{
    public required MediaInfo Media { get; init; }
    public required TargetProfile Profile { get; init; }
    public required PrepAction Action { get; init; }

    /// <summary>Pasos en lenguaje humano: "Recodificar a HAP Alpha", "Normalizar audio a -23 LUFS"…</summary>
    public IReadOnlyList<string> Steps { get; init; } = Array.Empty<string>();

    /// <summary>Nombre del archivo de salida (sin ruta).</summary>
    public required string OutputName { get; init; }

    /// <summary>Ajustes de codificación. null cuando la acción es Copy/Rename/Skip.</summary>
    public EncodeSpec? Encode { get; init; }

    /// <summary>Motivo cuando la acción es Skip.</summary>
    public string SkipReason { get; init; } = "";

    /// <summary>El usuario puede desmarcar el archivo en el paso 4.</summary>
    public bool Selected { get; set; } = true;
}
