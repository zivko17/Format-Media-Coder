namespace FormatMediaCoder.Core.Models;

/// <summary>Gravedad de una desviación respecto al perfil (brief §4.2).</summary>
public enum Severity
{
    /// <summary>Cosmético: nombre, orden. No afecta a la reproducción.</summary>
    Cosmetic = 0,
    /// <summary>Avisa: reproducirá mal o con riesgo.</summary>
    Warns = 1,
    /// <summary>Bloquea: no reproducirá.</summary>
    Blocks = 2,
}

/// <summary>Semáforo por archivo que se pinta en el diagnóstico (paso 3).</summary>
public enum ComplianceStatus
{
    /// <summary>Verde: cumple, se copia tal cual.</summary>
    Ok,
    /// <summary>Ámbar: avisa.</summary>
    Warn,
    /// <summary>Rojo: bloquea.</summary>
    Block,
    /// <summary>Gris: no se puede juzgar (perfil incompleto). Honesto, no inventado.</summary>
    Unknown,
}

/// <summary>Qué campo del archivo incumple.</summary>
public enum DeviationField
{
    Codec,
    Container,
    PixelFormat,
    Resolution,
    Fps,
    Audio,
    Naming,
    Limits,
}

/// <summary>
/// Una desviación concreta: qué incumple, cuánto importa, qué se haría y si se
/// puede arreglar solo. El lenguaje del mensaje es humano, no jerga: "se verá a
/// tirones en Resolume", no "pix_fmt yuv420p10le no soportado" (brief §6.3).
/// </summary>
public sealed class Deviation
{
    public required DeviationField Field { get; init; }
    public required Severity Severity { get; init; }

    /// <summary>Qué pasa, en lenguaje claro.</summary>
    public required string Message { get; init; }

    /// <summary>Detalle técnico, para quien lo quiera ver.</summary>
    public string Detail { get; init; } = "";

    /// <summary>Qué se haría para arreglarlo, en lenguaje claro.</summary>
    public string Fix { get; init; } = "";

    /// <summary>¿Reparable automáticamente o necesita decisión humana?</summary>
    public bool AutoFixable { get; init; }
}

/// <summary>
/// Resultado de comparar un archivo con un perfil. Es lo que ComplianceService
/// devuelve: lógica pura, entra <see cref="MediaInfo"/> + perfil, sale esto.
/// </summary>
public sealed class ComplianceReport
{
    public required MediaInfo Media { get; init; }
    public required TargetProfile Profile { get; init; }
    public required ComplianceStatus Status { get; init; }
    public IReadOnlyList<Deviation> Deviations { get; init; } = Array.Empty<Deviation>();

    /// <summary>Motivo cuando el estado es Unknown (perfil incompleto).</summary>
    public string UnknownReason { get; init; } = "";

    public bool Complies => Status == ComplianceStatus.Ok;
    public bool HasBlocking => Deviations.Any(d => d.Severity == Severity.Blocks);

    /// <summary>¿Todas las desviaciones se pueden arreglar solas? Falso si algo bloquea sin arreglo.</summary>
    public bool FullyAutoFixable =>
        Status != ComplianceStatus.Unknown &&
        Deviations.All(d => d.AutoFixable);
}
