using FormatMediaCoder.Core.Models;

namespace FormatMediaCoder.Core.Services;

/// <summary>
/// EL MOTOR DE CUMPLIMIENTO. Recibe un archivo analizado y un perfil de destino
/// y devuelve la lista de desviaciones. Es el corazón del producto y la única
/// pieza que es lógica pura: entra <see cref="MediaInfo"/> + perfil, sale un
/// <see cref="ComplianceReport"/>. Se prueba sin tocar disco ni FFmpeg (brief §4.2, §7).
///
/// Regla de oro (brief §6.3): mensajes en lenguaje humano, no jerga.
/// </summary>
public static class ComplianceService
{
    public static ComplianceReport Evaluate(MediaInfo media, TargetProfile profile)
    {
        // Un perfil incompleto no juzga: dice por qué no puede, en vez de inventar.
        if (!profile.Complete)
        {
            return new ComplianceReport
            {
                Media = media,
                Profile = profile,
                Status = ComplianceStatus.Unknown,
                UnknownReason = string.IsNullOrWhiteSpace(profile.IncompleteReason)
                    ? "El perfil de destino está incompleto: faltan especificaciones confirmadas."
                    : profile.IncompleteReason,
            };
        }

        var deviations = new List<Deviation>();

        CheckContainer(media, profile, deviations);
        CheckVideoCodec(media, profile, deviations);
        CheckPixelFormat(media, profile, deviations);
        CheckResolution(media, profile, deviations);
        CheckFps(media, profile, deviations);
        CheckAudio(media, profile, deviations);
        CheckLimits(media, profile, deviations);
        CheckNaming(media, profile, deviations);

        var status = deviations.Count == 0
            ? ComplianceStatus.Ok
            : deviations.Any(d => d.Severity == Severity.Blocks)
                ? ComplianceStatus.Block
                : deviations.Any(d => d.Severity == Severity.Warns)
                    ? ComplianceStatus.Warn
                    : ComplianceStatus.Ok; // solo cosméticas ⇒ sigue siendo "verde", se prepara sin fricción

        // Si solo hay cosméticas, el estado es Ok pero conservamos las desviaciones
        // para que el plan pueda renombrar. El semáforo verde es honesto: reproducirá bien.
        return new ComplianceReport
        {
            Media = media,
            Profile = profile,
            Status = status,
            Deviations = deviations,
        };
    }

    // ── Contenedor ─────────────────────────────────────────────────────────
    private static void CheckContainer(MediaInfo m, TargetProfile p, List<Deviation> outp)
    {
        var want = p.Video.Container;
        if (string.IsNullOrEmpty(want)) return;

        // ffprobe lista varios contenedores para el mismo archivo (mov,mp4,m4a…).
        // Nos basta con que el contenedor pedido esté entre los declarados o sea la extensión.
        bool ok = m.Extension.Equals(want, StringComparison.OrdinalIgnoreCase)
                  || m.ContainerFormat.Split(',').Any(f => f.Trim().Equals(want, StringComparison.OrdinalIgnoreCase));

        if (!ok)
        {
            outp.Add(new Deviation
            {
                Field = DeviationField.Container,
                Severity = Severity.Blocks,
                Message = $"El contenedor no es .{want}, que es el que espera {p.System}.",
                Detail = $"contenedor actual: {(string.IsNullOrEmpty(m.ContainerFormat) ? m.Extension : m.ContainerFormat)} → objetivo: {want}",
                Fix = $"Reempaquetar/recodificar a .{want}.",
                AutoFixable = true,
            });
        }
    }

    // ── Codec de vídeo ─────────────────────────────────────────────────────
    private static void CheckVideoCodec(MediaInfo m, TargetProfile p, List<Deviation> outp)
    {
        var want = p.Video.Codec;
        if (string.IsNullOrEmpty(want)) return;

        if (!m.HasVideo)
        {
            outp.Add(new Deviation
            {
                Field = DeviationField.Codec,
                Severity = Severity.Blocks,
                Message = "El archivo no tiene pista de vídeo y el destino la necesita.",
                Detail = "sin stream de vídeo",
                Fix = "Revisar el archivo de origen.",
                AutoFixable = false,
            });
            return;
        }

        if (!m.Video!.Codec.Equals(want, StringComparison.OrdinalIgnoreCase))
        {
            outp.Add(new Deviation
            {
                Field = DeviationField.Codec,
                Severity = Severity.Blocks,
                Message = $"El vídeo está en {m.Video.Codec.ToUpperInvariant()} y {p.System} no lo reproduce; necesita {DescribeVariant(p)}.",
                Detail = $"codec actual: {m.Video.Codec} → objetivo: {want}{(p.Video.Variant is null ? "" : " (" + p.Video.Variant + ")")}",
                Fix = $"Recodificar a {DescribeVariant(p)}.",
                AutoFixable = true,
            });
        }
    }

    // ── Pixel format / alfa ────────────────────────────────────────────────
    private static void CheckPixelFormat(MediaInfo m, TargetProfile p, List<Deviation> outp)
    {
        if (!m.HasVideo) return;

        // Caso crítico del brief §5: el destino usa HAP Alpha (conserva transparencia)
        // pero se recodifica sin ella, o al revés. Solo tiene sentido comparar cuando
        // el codec ya coincide; si el codec difiere, la recodificación ya fija el pixel format.
        bool codecMatches = !string.IsNullOrEmpty(p.Video.Codec)
                            && m.Video!.Codec.Equals(p.Video.Codec, StringComparison.OrdinalIgnoreCase);
        if (!codecMatches) return;

        bool wantsAlpha = p.Video.Variant == "hap_alpha"
                          || (p.Video.PixelFormat?.Contains('a') ?? false);

        if (wantsAlpha && !m.HasAlpha)
        {
            outp.Add(new Deviation
            {
                Field = DeviationField.PixelFormat,
                Severity = Severity.Warns,
                Message = "El destino es HAP Alpha (transparencia) pero el archivo no trae canal alfa.",
                Detail = $"pixel format actual: {m.Video!.PixelFormat} (sin alfa)",
                Fix = "Si no necesitas transparencia, usa un perfil HAP Q (más calidad, sin alfa).",
                AutoFixable = false,
            });
        }
        else if (!wantsAlpha && m.HasAlpha)
        {
            outp.Add(new Deviation
            {
                Field = DeviationField.PixelFormat,
                Severity = Severity.Warns,
                Message = "El archivo tiene transparencia, pero este perfil la descarta.",
                Detail = $"pixel format actual: {m.Video!.PixelFormat} (con alfa) → variante {p.Video.Variant} sin alfa",
                Fix = "Si necesitas conservar los logos/capas transparentes, usa el perfil HAP Alpha.",
                AutoFixable = false,
            });
        }
    }

    // ── Resolución / geometría ─────────────────────────────────────────────
    private static void CheckResolution(MediaInfo m, TargetProfile p, List<Deviation> outp)
    {
        if (!m.HasVideo) return;
        var accepted = p.Geometry.AcceptedResolutions;
        if (accepted.Count == 0) return; // vacío = cualquiera

        var current = new Resolution(m.Video!.Width, m.Video.Height);
        if (accepted.Contains(current)) return;

        // Decisión del brief §9c: por defecto AVISAR Y NO TOCAR. La política del
        // perfil decide si además es auto-reparable (pad/scale/crop) o no (warn).
        bool autoFix = p.Geometry.Policy != GeometryPolicy.Warn;
        string acc = string.Join(", ", accepted.Select(r => r.ToString()));

        outp.Add(new Deviation
        {
            Field = DeviationField.Resolution,
            Severity = Severity.Warns,
            Message = $"La resolución {current} no encaja con el mapeo de {p.System}.",
            Detail = $"actual: {current} → admitidas: {acc} · política: {PolicyLabel(p.Geometry.Policy)}",
            Fix = autoFix ? $"Ajustar geometría ({PolicyLabel(p.Geometry.Policy)}) al destino." : "Marcado para que lo decidas: no se toca la imagen automáticamente.",
            AutoFixable = autoFix,
        });
    }

    // ── fps ────────────────────────────────────────────────────────────────
    private static void CheckFps(MediaInfo m, TargetProfile p, List<Deviation> outp)
    {
        if (!m.HasVideo) return;
        var accepted = p.Temporal.AcceptedFps;
        if (accepted.Count == 0) return;

        // Tolerancia para el eterno 29.97 ≈ 30, 23.976 ≈ 24.
        bool ok = accepted.Any(f => Math.Abs(f - m.Video!.Fps) < 0.05);
        if (ok) return;

        bool autoFix = p.Temporal.Policy == TemporalPolicy.Conform;
        string acc = string.Join(", ", accepted.Select(f => Trim(f)));

        outp.Add(new Deviation
        {
            Field = DeviationField.Fps,
            Severity = Severity.Warns,
            Message = $"Va a {Trim(m.Video!.Fps)} fps y {p.System} espera {acc} fps; puede verse a tirones.",
            Detail = $"actual: {Trim(m.Video.Fps)} fps → admitidos: {acc} fps",
            Fix = autoFix ? "Conformar los fps al destino." : "Marcado para que lo decidas.",
            AutoFixable = autoFix,
        });
    }

    // ── Audio ──────────────────────────────────────────────────────────────
    private static void CheckAudio(MediaInfo m, TargetProfile p, List<Deviation> outp)
    {
        if (!p.Audio.Wanted)
        {
            // El destino no quiere audio; que lo tenga o no es indiferente (no se marca).
            return;
        }

        if (!m.HasAudio)
        {
            outp.Add(new Deviation
            {
                Field = DeviationField.Audio,
                Severity = Severity.Warns,
                Message = "El destino quiere audio y el archivo no tiene.",
                Detail = "sin stream de audio",
                Fix = "Añadir/comprobar la pista de audio en el origen.",
                AutoFixable = false,
            });
            return;
        }

        if (!string.IsNullOrEmpty(p.Audio.Codec) &&
            !m.Audio!.Codec.Equals(p.Audio.Codec, StringComparison.OrdinalIgnoreCase))
        {
            outp.Add(new Deviation
            {
                Field = DeviationField.Audio,
                Severity = Severity.Warns,
                Message = $"El audio está en {m.Audio.Codec.ToUpperInvariant()} y el destino pide {p.Audio.Codec.ToUpperInvariant()}.",
                Detail = $"codec de audio actual: {m.Audio.Codec} → objetivo: {p.Audio.Codec}",
                Fix = $"Recodificar el audio a {p.Audio.Codec}.",
                AutoFixable = true,
            });
        }
    }

    // ── Límites duros ──────────────────────────────────────────────────────
    private static void CheckLimits(MediaInfo m, TargetProfile p, List<Deviation> outp)
    {
        var l = p.Limits;
        if (m.HasVideo && l.MaxWidth is int mw && l.MaxHeight is int mh &&
            (m.Video!.Width > mw || m.Video.Height > mh))
        {
            outp.Add(new Deviation
            {
                Field = DeviationField.Limits,
                Severity = Severity.Blocks,
                Message = $"La resolución {m.Video.ResolutionLabel} supera el máximo que admite {p.System} ({mw}×{mh}).",
                Detail = $"actual: {m.Video.ResolutionLabel} → máximo: {mw}×{mh}",
                Fix = $"Escalar por debajo de {mw}×{mh}.",
                AutoFixable = true,
            });
        }

        if (l.MaxFileSize is long ms && m.SizeBytes > ms)
        {
            outp.Add(new Deviation
            {
                Field = DeviationField.Limits,
                Severity = Severity.Warns,
                Message = "El archivo pesa más de lo que admite el destino.",
                Detail = $"actual: {m.SizeBytes} B → máximo: {ms} B",
                Fix = "Comprimir o recodificar más agresivo.",
                AutoFixable = true,
            });
        }
    }

    // ── Nomenclatura ───────────────────────────────────────────────────────
    private static void CheckNaming(MediaInfo m, TargetProfile p, List<Deviation> outp)
    {
        // Solo cosmético y solo si el perfil define un patrón (brief §9d: el patrón
        // exacto está pendiente de confirmar, así que de momento no forzamos nada).
        if (string.IsNullOrEmpty(p.Naming.Pattern)) return;
        // El patrón concreto se resolverá cuando el autor lo confirme (§9d).
    }

    // ── Ayudas ─────────────────────────────────────────────────────────────
    private static string DescribeVariant(TargetProfile p)
    {
        if (p.Video.Codec == "hap")
        {
            return p.Video.Variant switch
            {
                "hap_alpha" => "HAP Alpha (con transparencia)",
                "hap_q" => "HAP Q (máxima calidad, sin alfa)",
                _ => "HAP",
            };
        }
        return p.Video.Codec?.ToUpperInvariant() ?? "el codec del destino";
    }

    private static string PolicyLabel(GeometryPolicy pol) => pol switch
    {
        GeometryPolicy.Pad => "rellenar con barras",
        GeometryPolicy.Scale => "escalar",
        GeometryPolicy.Crop => "recortar por el centro",
        _ => "avisar y no tocar",
    };

    private static string Trim(double v) =>
        v % 1 == 0 ? ((int)v).ToString() : v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
