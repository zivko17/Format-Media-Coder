using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using FormatMediaCoder.Core.Models;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>
/// Una fila del PASO 3 (diagnóstico): un archivo con su estado de cumplimiento y
/// su plan. Verde cumple, ámbar avisa, rojo bloquea, gris no se puede juzgar
/// (brief §6.1). Al pinchar se ve el detalle de qué falla y por qué.
/// </summary>
public sealed class FileRowViewModel : ObservableObject
{
    public MediaInfo Media { get; }
    public ComplianceReport Report { get; }
    public PrepPlan Plan { get; }

    public FileRowViewModel(ComplianceReport report, PrepPlan plan)
    {
        Media = report.Media;
        Report = report;
        Plan = plan;
        _selected = plan.Action is not (PrepAction.Skip);
    }

    public string FileName => Media.FileName;

    public string ResolutionLabel => Media.Video is { } v ? v.ResolutionLabel : "—";
    public string CodecLabel => Media.Video?.Codec.ToUpperInvariant() ?? (Media.HasAudio ? "solo audio" : "—");

    private bool _selected;
    /// <summary>El usuario puede desmarcar el archivo en el paso 4 (brief §4.3).</summary>
    public bool Selected
    {
        get => _selected;
        set { if (Set(ref _selected, value)) Plan.Selected = value; }
    }

    /// <summary>Se puede seleccionar solo si hay algo que hacer (no en Skip).</summary>
    public bool CanSelect => Plan.Action != PrepAction.Skip;

    public string StatusText => Report.Status switch
    {
        ComplianceStatus.Ok => "Listo",
        ComplianceStatus.Warn => "Aviso",
        ComplianceStatus.Block => "Bloquea",
        _ => "Sin juzgar",
    };

    /// <summary>Color del semáforo del paso 3 (tonos desaturados, sobrios).</summary>
    public Brush StatusBrush => new SolidColorBrush(Report.Status switch
    {
        ComplianceStatus.Ok => Color.FromArgb(0xFF, 0x5f, 0x9e, 0x6b),   // verde
        ComplianceStatus.Warn => Color.FromArgb(0xFF, 0xb8, 0x92, 0x3f), // ámbar
        ComplianceStatus.Block => Color.FromArgb(0xFF, 0xbd, 0x64, 0x59),// rojo
        _ => Color.FromArgb(0xFF, 0x6c, 0x6e, 0x70),                     // gris
    });

    /// <summary>Qué se va a hacer, en una línea.</summary>
    public string ActionSummary => Plan.Action switch
    {
        PrepAction.Copy => "Copiar tal cual (ya cumple)",
        PrepAction.Recode => "Recodificar",
        PrepAction.Rename => "Renombrar",
        PrepAction.Skip => "No se puede solo — " + Plan.SkipReason,
        _ => "",
    };

    /// <summary>Detalle expandible: lista de desviaciones y pasos del plan.</summary>
    public IEnumerable<string> Details =>
        Report.Status == ComplianceStatus.Unknown
            ? new[] { Report.UnknownReason }
            : Report.Deviations.Select(d => $"• {d.Message}  →  {d.Fix}")
                .Concat(Plan.Steps.Select(s => "· " + s));
}
