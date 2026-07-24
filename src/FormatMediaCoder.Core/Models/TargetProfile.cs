namespace FormatMediaCoder.Core.Models;

/// <summary>Qué hacer cuando la geometría del clip no encaja con el destino.</summary>
public enum GeometryPolicy
{
    /// <summary>Avisar y NO tocar la imagen. El usuario decide. (Por defecto, brief §9c.)</summary>
    Warn,
    /// <summary>Encajar dentro del lienzo con barras (letterbox/pillarbox). No deforma ni recorta.</summary>
    Pad,
    /// <summary>Estirar al tamaño destino. Deforma.</summary>
    Scale,
    /// <summary>Rellenar el lienzo recortando lo que sobra. Pierde bordes.</summary>
    Crop,
}

/// <summary>Qué hacer cuando los fps no encajan.</summary>
public enum TemporalPolicy
{
    Warn,
    Conform,
}

/// <summary>
/// PERFIL DE DESTINO. Datos, no código. Describe qué acepta un sistema de
/// reproducción de show. Es la unidad central del rediseño: la app deja de
/// preguntar "¿qué operación quieres?" y pregunta "¿a qué DESTINO va esto?".
/// (Brief §3 y §4.1)
///
/// Un perfil incompleto (<see cref="Complete"/> = false) es honesto sobre lo
/// que no sabe: el motor se niega a juzgar con él en vez de inventar un
/// veredicto. Un perfil con datos a ojo es peor que no tener perfil, porque
/// falla el día del evento.
/// </summary>
public sealed class TargetProfile
{
    // ── Identidad ──────────────────────────────────────────────────────────
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string System { get; set; } = "";
    public string Notes { get; set; } = "";

    /// <summary>false ⇒ faltan specs confirmadas; no se juzga con él.</summary>
    public bool Complete { get; set; } = true;
    public string IncompleteReason { get; set; } = "";

    /// <summary>Campos cuyo valor es una suposición pendiente de confirmar (brief §9a).</summary>
    public List<string> Unconfirmed { get; set; } = new();

    /// <summary>true en los perfiles de fábrica (no editables/borrables).</summary>
    public bool Builtin { get; set; }

    // ── Especificaciones ───────────────────────────────────────────────────
    public VideoSpec Video { get; set; } = new();
    public GeometrySpec Geometry { get; set; } = new();
    public TemporalSpec Temporal { get; set; } = new();
    public AudioSpec Audio { get; set; } = new();
    public NamingSpec Naming { get; set; } = new();
    public LimitsSpec Limits { get; set; } = new();

    public sealed class VideoSpec
    {
        public string? Codec { get; set; }
        public string? Container { get; set; }
        /// <summary>Variante del codec. En HAP: "hap" | "hap_alpha" | "hap_q". Campo fijo del perfil (brief §9a).</summary>
        public string? Variant { get; set; }
        public string? PixelFormat { get; set; }
    }

    public sealed class GeometrySpec
    {
        /// <summary>Resoluciones admitidas. Vacío = cualquiera.</summary>
        public List<Resolution> AcceptedResolutions { get; set; } = new();
        public GeometryPolicy Policy { get; set; } = GeometryPolicy.Warn;
    }

    public sealed class TemporalSpec
    {
        /// <summary>fps admitidos. Vacío = cualquiera.</summary>
        public List<double> AcceptedFps { get; set; } = new();
        public TemporalPolicy Policy { get; set; } = TemporalPolicy.Warn;
    }

    public sealed class AudioSpec
    {
        public bool Wanted { get; set; }
        public string? Codec { get; set; }
        public int? Channels { get; set; }
        public int? SampleRate { get; set; }
        /// <summary>LUFS objetivo (null = no normalizar). Por defecto -23 (EBU R128, brief §9e).</summary>
        public double? TargetLoudness { get; set; }
    }

    public sealed class NamingSpec
    {
        /// <summary>Patrón de nombre de salida. null = conservar el original.</summary>
        public string? Pattern { get; set; }
        public bool Number { get; set; }
    }

    public sealed class LimitsSpec
    {
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }
        public long? MaxBitrate { get; set; }
        public long? MaxFileSize { get; set; }
    }
}

/// <summary>Par ancho×alto. Struct de valor para comparar resoluciones sin sorpresas.</summary>
public readonly record struct Resolution(int Width, int Height)
{
    public override string ToString() => $"{Width}×{Height}";
}
