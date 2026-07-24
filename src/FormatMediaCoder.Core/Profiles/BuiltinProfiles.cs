using FormatMediaCoder.Core.Models;

namespace FormatMediaCoder.Core.Profiles;

/// <summary>
/// Perfiles de fábrica. Estado de cada uno (brief §4.1):
///   Resolume Arena .... HAP / HAP Alpha. Conocido, implementado y verificado.
///   Watchout .......... HAP. Confirmado por el autor; variante/límites sin confirmar (§9a).
///   vMix / OBS ........ POR DEFINIR (§9b).
///   Reproductor LED ... POR DEFINIR, varía por fabricante (§9b).
///
/// Que Resolume y Watchout compartan HAP es la gran noticia del proyecto: un
/// solo perfil bien hecho cubre los dos servidores de show (brief §5).
///
/// NO INVENTAR ESPECIFICACIONES: los perfiles sin confirmar quedan marcados como
/// incompletos y el motor se niega a juzgar con ellos.
/// </summary>
public static class BuiltinProfiles
{
    /// <summary>Nivel de audio broadcast estándar (EBU R128). Decisión del brief §9e: se mantiene.</summary>
    public const double DefaultLoudness = -23;

    public static IReadOnlyList<TargetProfile> All() => new[]
    {
        ResolumeHapAlpha(),
        ResolumeHapQ(),
        WatchoutHap(),
        VmixObs(),
        LedPlayer(),
    };

    private static TargetProfile ResolumeHapAlpha() => new()
    {
        Id = "resolume-hap-alpha",
        Name = "Resolume Arena — HAP Alpha",
        System = "Resolume Arena",
        Notes = "Con canal alfa (DXT5). Para logos y capas superpuestas que viven de la transparencia.",
        Complete = true,
        Builtin = true,
        Video = new() { Codec = "hap", Container = "mov", Variant = "hap_alpha", PixelFormat = "rgba" },
        Geometry = new() { Policy = GeometryPolicy.Warn }, // Resolume acepta resoluciones arbitrarias
        Temporal = new() { Policy = TemporalPolicy.Warn },
        Audio = new() { Wanted = false, TargetLoudness = DefaultLoudness },
        Naming = new(),
        Limits = new(),
    };

    private static TargetProfile ResolumeHapQ() => new()
    {
        Id = "resolume-hap-q",
        Name = "Resolume Arena — HAP Q",
        System = "Resolume Arena",
        Notes = "Máxima calidad, sin canal alfa (YCoCg DXT5). Para clips opacos a pantalla completa.",
        Complete = true,
        Builtin = true,
        Video = new() { Codec = "hap", Container = "mov", Variant = "hap_q", PixelFormat = "rgb24" },
        Geometry = new() { Policy = GeometryPolicy.Warn },
        Temporal = new() { Policy = TemporalPolicy.Warn },
        Audio = new() { Wanted = false, TargetLoudness = DefaultLoudness },
        Naming = new(),
        Limits = new(),
    };

    private static TargetProfile WatchoutHap() => new()
    {
        Id = "watchout-hap",
        Name = "Watchout — HAP",
        System = "Watchout",
        Notes = "HAP confirmado por el autor. Quedan por confirmar variante, límites de resolución y fps (brief §9a).",
        Complete = true,
        Unconfirmed = { "variant", "resolution", "fps" },
        Builtin = true,
        Video = new() { Codec = "hap", Container = "mov", Variant = "hap", PixelFormat = "rgb24" },
        Geometry = new() { Policy = GeometryPolicy.Warn },
        Temporal = new() { Policy = TemporalPolicy.Warn },
        Audio = new() { Wanted = false, TargetLoudness = DefaultLoudness },
        Naming = new(),
        Limits = new(),
    };

    private static TargetProfile VmixObs() => new()
    {
        Id = "vmix-obs",
        Name = "vMix / OBS — por definir",
        System = "vMix / OBS",
        Notes = "Faltan las especificaciones confirmadas por el autor (brief §9b).",
        Complete = false,
        IncompleteReason = "Perfil sin definir: faltan codec, contenedor, resoluciones, fps y pixel format confirmados (brief §9b). NO se inventan.",
        Builtin = true,
    };

    private static TargetProfile LedPlayer() => new()
    {
        Id = "led-player",
        Name = "Reproductor LED — por definir",
        System = "Reproductor integrado de pantalla LED",
        Notes = "El que más restricciones suele tener y el que más varía por fabricante. Faltan specs (brief §9b).",
        Complete = false,
        IncompleteReason = "Perfil sin definir: cada procesador/pantalla LED impone sus propios límites. Hay que confirmarlos con el fabricante antes de escribir el perfil (brief §9b).",
        Builtin = true,
    };
}
