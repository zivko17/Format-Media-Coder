using System.Globalization;
using FormatMediaCoder.Core.Models;

namespace FormatMediaCoder.Core.Services;

/// <summary>
/// Construcción de los argumentos de FFmpeg a partir de un <see cref="PrepPlan"/>.
/// Lógica PURA (sin proceso ni disco) para poder testear el punto más delicado
/// del rediseño: que la rama HAP lleve el <c>-format</c> correcto y no descarte
/// la transparencia (brief §5).
/// </summary>
public static class FfmpegArgs
{
    /// <summary>Argumentos completos de FFmpeg para ejecutar el re-encode de un plan.</summary>
    public static List<string> BuildEncodeArgs(PrepPlan plan, string outputPath)
    {
        var e = plan.Encode ?? new EncodeSpec();
        var args = new List<string> { "-y", "-i", plan.Media.FilePath };

        var vf = new List<string>();
        if (e.Scale is Resolution r)
            vf.Add($"scale={r.Width}:{r.Height}");

        if (!string.IsNullOrEmpty(e.VideoCodec))
        {
            if (e.VideoCodec == "hap")
            {
                // ── CORRECCIÓN DEL BUG DEL ALFA (brief §5) ───────────────────
                // El encoder hap admite tres variantes y por defecto sale 'hap'
                // (DXT1, SIN alfa). Aunque se le entregue una imagen RGBA, la
                // transparencia se pierde salvo que se pase -format explícito.
                var variant = string.IsNullOrEmpty(e.HapVariant) ? "hap" : e.HapVariant;

                // El pixel format de entrada debe casar con la variante.
                vf.Add(variant == "hap_alpha" ? "format=rgba" : "format=rgb24");

                args.Add("-c:v");
                args.Add("hap");
                if (variant != "hap")
                {
                    args.Add("-format");
                    args.Add(variant); // hap_alpha (con alfa) | hap_q (máx. calidad sin alfa)
                }
            }
            else if (e.VideoCodec == "copy")
            {
                args.Add("-c:v"); args.Add("copy");
            }
            else
            {
                args.Add("-c:v"); args.Add(e.VideoCodec);
                if (!string.IsNullOrEmpty(e.PixelFormat))
                {
                    args.Add("-pix_fmt"); args.Add(e.PixelFormat);
                }
            }
        }

        if (vf.Count > 0)
        {
            args.Add("-vf");
            args.Add(string.Join(",", vf));
        }

        if (e.Fps is double fps)
        {
            args.Add("-r");
            args.Add(fps.ToString("0.###", CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(e.AudioCodec))
        {
            args.Add("-c:a"); args.Add(e.AudioCodec);
        }
        if (e.NormalizeLoudness is double lufs)
        {
            // EBU R128 (brief §9e). LRA y TP en valores broadcast estándar.
            args.Add("-af");
            args.Add($"loudnorm=I={lufs.ToString("0.#", CultureInfo.InvariantCulture)}:LRA=7:TP=-2.0");
        }

        args.Add(outputPath);
        return args;
    }
}
