using FormatMediaCoder.Core.Models;

namespace FormatMediaCoder.Core.Services;

/// <summary>
/// Convierte un <see cref="ComplianceReport"/> en un <see cref="PrepPlan"/>: por
/// cada archivo, qué se va a hacer. Regla clave del brief §4.3: si un archivo YA
/// cumple, se COPIA tal cual, no se recodifica. Recodificar por costumbre pierde
/// calidad y tiempo.
///
/// Lógica pura: entra el informe, sale el plan. Testeable sin disco ni FFmpeg.
/// </summary>
public static class PrepPlanBuilder
{
    public static PrepPlan Build(ComplianceReport report)
    {
        var media = report.Media;
        var profile = report.Profile;

        // Perfil incompleto ⇒ no se puede decidir. Honesto, no inventado.
        if (report.Status == ComplianceStatus.Unknown)
        {
            return new PrepPlan
            {
                Media = media,
                Profile = profile,
                Action = PrepAction.Skip,
                OutputName = media.FileName,
                SkipReason = string.IsNullOrEmpty(report.UnknownReason)
                    ? "Perfil de destino incompleto."
                    : report.UnknownReason,
            };
        }

        var deviations = report.Deviations;

        // Cumple del todo ⇒ COPY. No se recodifica.
        if (deviations.Count == 0)
        {
            return new PrepPlan
            {
                Media = media,
                Profile = profile,
                Action = PrepAction.Copy,
                OutputName = OutputNameFor(media, profile, recoded: false),
                Steps = new[] { "Ya cumple: copiar tal cual (sin recodificar)." },
            };
        }

        // ¿Qué desviaciones exigen re-encode? (codec, contenedor, pixel format,
        // resolución/fps si su política es auto-arreglar, límites de resolución).
        bool needsReencode = deviations.Any(d =>
            d.AutoFixable &&
            d.Field is DeviationField.Codec or DeviationField.Container
                    or DeviationField.PixelFormat or DeviationField.Audio
                    or DeviationField.Limits
            || (d.AutoFixable && d.Field is DeviationField.Resolution or DeviationField.Fps));

        // Bloqueos que NO se pueden arreglar solos ⇒ Skip (el usuario debe intervenir).
        bool hasUnfixableBlock = deviations.Any(d => d.Severity == Severity.Blocks && !d.AutoFixable);
        if (hasUnfixableBlock)
        {
            return new PrepPlan
            {
                Media = media,
                Profile = profile,
                Action = PrepAction.Skip,
                OutputName = media.FileName,
                SkipReason = string.Join(" ", deviations
                    .Where(d => d.Severity == Severity.Blocks && !d.AutoFixable)
                    .Select(d => d.Message)),
                Steps = deviations.Select(HumanStep).ToList(),
            };
        }

        if (!needsReencode)
        {
            // Solo quedan desviaciones no reparables por re-encode (p.ej. geometría
            // en política "avisar y no tocar", o avisos de alfa). No tocamos el
            // archivo: se copia y se deja constancia de los avisos.
            return new PrepPlan
            {
                Media = media,
                Profile = profile,
                Action = PrepAction.Copy,
                OutputName = OutputNameFor(media, profile, recoded: false),
                Steps = new[] { "Cumple lo esencial: copiar tal cual." }
                        .Concat(deviations.Select(d => "Aviso: " + d.Message))
                        .ToList(),
            };
        }

        // ── Construir el EncodeSpec a partir de las desviaciones auto-reparables ──
        var steps = new List<string>();
        string? videoCodec = null, hapVariant = null, pixelFormat = null, container = null;
        Resolution? scale = null;
        double? fps = null;
        string? audioCodec = null;
        double? loudness = null;

        foreach (var d in deviations)
        {
            switch (d.Field)
            {
                case DeviationField.Codec when d.AutoFixable:
                case DeviationField.PixelFormat when d.AutoFixable:
                    videoCodec = profile.Video.Codec;
                    hapVariant = profile.Video.Codec == "hap" ? profile.Video.Variant : null;
                    pixelFormat = profile.Video.PixelFormat;
                    steps.Add($"Recodificar vídeo a {VariantLabel(profile)}.");
                    break;

                case DeviationField.Container when d.AutoFixable:
                    container = profile.Video.Container;
                    break;

                case DeviationField.Resolution when d.AutoFixable:
                    if (profile.Geometry.AcceptedResolutions.Count > 0)
                    {
                        scale = profile.Geometry.AcceptedResolutions[0];
                        steps.Add($"Ajustar geometría a {scale} ({PolicyLabel(profile.Geometry.Policy)}).");
                    }
                    break;

                case DeviationField.Fps when d.AutoFixable:
                    if (profile.Temporal.AcceptedFps.Count > 0)
                    {
                        fps = profile.Temporal.AcceptedFps[0];
                        steps.Add($"Conformar a {Trim(fps.Value)} fps.");
                    }
                    break;

                case DeviationField.Audio when d.AutoFixable:
                    audioCodec = profile.Audio.Codec;
                    steps.Add($"Recodificar audio a {profile.Audio.Codec}.");
                    break;

                case DeviationField.Limits when d.AutoFixable:
                    if (profile.Limits.MaxWidth is int mw && profile.Limits.MaxHeight is int mh)
                    {
                        scale = new Resolution(mw, mh);
                        steps.Add($"Escalar por debajo del máximo ({mw}×{mh}).");
                    }
                    break;
            }
        }

        // Si el destino quiere codec de vídeo y aún no lo hemos fijado (p.ej. solo
        // falla el contenedor), forzamos el codec del perfil para el re-encode.
        if (videoCodec is null && !string.IsNullOrEmpty(profile.Video.Codec) && media.HasVideo)
        {
            videoCodec = profile.Video.Codec;
            hapVariant = profile.Video.Codec == "hap" ? profile.Video.Variant : null;
            pixelFormat = profile.Video.PixelFormat;
        }

        // Normalización de audio: si el destino define un objetivo de loudness y hay
        // audio, se aplica en el re-encode. -23 LUFS por defecto (brief §9e).
        if (profile.Audio.Wanted && media.HasAudio && profile.Audio.TargetLoudness is double t)
        {
            loudness = t;
            steps.Add($"Normalizar audio a {Trim(t)} LUFS (EBU R128).");
        }

        // Avisos que NO se tocan (geometría en política warn, alfa) se dejan escritos.
        foreach (var d in deviations.Where(d => !d.AutoFixable))
            steps.Add("Aviso: " + d.Message);

        return new PrepPlan
        {
            Media = media,
            Profile = profile,
            Action = PrepAction.Recode,
            OutputName = OutputNameFor(media, profile, recoded: true),
            Steps = steps,
            Encode = new EncodeSpec
            {
                VideoCodec = videoCodec,
                HapVariant = hapVariant,
                PixelFormat = pixelFormat,
                Container = container ?? profile.Video.Container,
                Scale = scale,
                Fps = fps,
                AudioCodec = audioCodec,
                NormalizeLoudness = loudness,
            },
        };
    }

    /// <summary>Nombre de salida. Nunca sobrescribe: la unicidad real la garantiza la capa de FFmpeg.</summary>
    private static string OutputNameFor(MediaInfo m, TargetProfile p, bool recoded)
    {
        var stem = System.IO.Path.GetFileNameWithoutExtension(m.FileName);
        // Patrón de nomenclatura del perfil pendiente de confirmar (brief §9d):
        // de momento conservamos el nombre y solo ajustamos la extensión al contenedor.
        var ext = recoded && !string.IsNullOrEmpty(p.Video.Container)
            ? p.Video.Container
            : m.Extension;
        return $"{stem}.{ext}";
    }

    private static string HumanStep(Deviation d) =>
        (d.AutoFixable ? "" : "Aviso: ") + (string.IsNullOrEmpty(d.Fix) ? d.Message : d.Fix);

    private static string VariantLabel(TargetProfile p)
    {
        if (p.Video.Codec == "hap")
            return p.Video.Variant switch
            {
                "hap_alpha" => "HAP Alpha (con transparencia)",
                "hap_q" => "HAP Q (máxima calidad, sin alfa)",
                _ => "HAP",
            };
        return p.Video.Codec?.ToUpperInvariant() ?? "codec destino";
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
