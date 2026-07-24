using System.Collections.ObjectModel;
using System.IO;
using FormatMediaCoder.App.Services;
using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>
/// La pantalla estrella: PREPARAR EVENTO. Cuatro pasos en una sola vista
/// (brief §6.1): entrada (carpeta) → destino (perfil) → diagnóstico → plan y
/// ejecución. El paso 3 es el que vende el producto (brief §7).
/// </summary>
public sealed class PrepararEventoViewModel : ObservableObject
{
    private readonly ProfileService _profiles;
    private readonly FFmpegService _ffmpeg;
    private CancellationTokenSource? _cts;

    public PrepararEventoViewModel()
    {
        _profiles = new ProfileService();
        _ffmpeg = FFmpegService.CreateDefault();
        _ffmpeg.Progress += (pct, tm) => { ProgressPercent = pct; ProgressTimemark = tm; };

        Profiles = new ObservableCollection<TargetProfile>(_profiles.LoadAll());
        SelectedProfile = Profiles.FirstOrDefault(p => p.Complete);

        ScanCommand = new RelayCommand(async () => await ScanAsync(), () => !string.IsNullOrEmpty(InputFolder) && SelectedProfile is not null && !IsBusy);
        ExecuteCommand = new RelayCommand(async () => await ExecuteAsync(), () => Files.Any(f => f.Selected) && !IsBusy);
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
    }

    // ── PASO 1: entrada ─────────────────────────────────────────────────────
    private string _inputFolder = "";
    public string InputFolder
    {
        get => _inputFolder;
        set { if (Set(ref _inputFolder, value)) ScanCommand.RaiseCanExecuteChanged(); }
    }

    // ── PASO 2: destino ─────────────────────────────────────────────────────
    public ObservableCollection<TargetProfile> Profiles { get; }

    private TargetProfile? _selectedProfile;
    public TargetProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (Set(ref _selectedProfile, value))
            {
                OnPropertyChanged(nameof(ProfileWarning));
                ScanCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Aviso honesto cuando el perfil elegido está incompleto o sin confirmar.</summary>
    public string ProfileWarning
    {
        get
        {
            if (SelectedProfile is null) return "";
            if (!SelectedProfile.Complete) return "⚠ " + SelectedProfile.IncompleteReason;
            if (SelectedProfile.Unconfirmed.Count > 0)
                return $"⚠ {SelectedProfile.Name}: pendiente de confirmar {string.Join(", ", SelectedProfile.Unconfirmed)} (brief §9a).";
            return "";
        }
    }

    // ── PASO 3: diagnóstico ─────────────────────────────────────────────────
    public ObservableCollection<FileRowViewModel> Files { get; } = new();

    private string _summary = "";
    public string Summary { get => _summary; private set => Set(ref _summary, value); }

    // ── PASO 4: ejecución ───────────────────────────────────────────────────
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (Set(ref _isBusy, value))
            {
                ScanCommand.RaiseCanExecuteChanged();
                ExecuteCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private double _progressPercent;
    public double ProgressPercent { get => _progressPercent; set => Set(ref _progressPercent, value); }

    private string _progressTimemark = "";
    public string ProgressTimemark { get => _progressTimemark; set => Set(ref _progressTimemark, value); }

    private string _statusLine = "";
    public string StatusLine { get => _statusLine; private set => Set(ref _statusLine, value); }

    public RelayCommand ScanCommand { get; }
    public RelayCommand ExecuteCommand { get; }
    public RelayCommand CancelCommand { get; }

    // ── Lógica ──────────────────────────────────────────────────────────────
    private async Task ScanAsync()
    {
        if (SelectedProfile is null) return;
        IsBusy = true;
        Files.Clear();
        Summary = "";

        try
        {
            var paths = MediaScanner.Scan(InputFolder, recurse: true);
            StatusLine = $"Analizando {paths.Count} archivo(s)…";

            int ready = 0, recode = 0, blocked = 0;
            foreach (var path in paths)
            {
                MediaInfo media;
                try { media = await _ffmpeg.ProbeAsync(path); }
                catch { continue; } // un archivo ilegible no tumba el análisis

                var report = ComplianceService.Evaluate(media, SelectedProfile);
                var plan = PrepPlanBuilder.Build(report);
                Files.Add(new FileRowViewModel(report, plan));

                switch (plan.Action)
                {
                    case PrepAction.Copy: ready++; break;
                    case PrepAction.Recode: recode++; break;
                    case PrepAction.Skip: blocked++; break;
                }
            }

            // Resumen del paso 3 (brief §6.1).
            Summary = $"{Files.Count} archivos · {ready} listos · {recode} hay que recodificar · {blocked} no se pueden arreglar solos";
            StatusLine = "";
            ExecuteCommand.RaiseCanExecuteChanged();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteAsync()
    {
        var selected = Files.Where(f => f.Selected && f.Plan.Action != PrepAction.Skip).ToList();
        if (selected.Count == 0) return;

        _cts = new CancellationTokenSource();
        IsBusy = true;

        // Carpeta de salida junto a la entrada, para no mezclar con el material del cliente.
        var outDir = Path.Combine(InputFolder, "_preparado");
        Directory.CreateDirectory(outDir);

        int done = 0, err = 0;
        try
        {
            foreach (var row in selected)
            {
                if (_cts.IsCancellationRequested) break;
                StatusLine = $"Preparando {row.FileName} ({done + 1}/{selected.Count})";
                ProgressPercent = 0;

                var outPath = PathUtils.UniquePath(Path.Combine(outDir, row.Plan.OutputName));
                try
                {
                    await _ffmpeg.ExecuteAsync(row.Plan, outPath, _cts.Token);
                    done++;
                }
                catch (OperationCanceledException) { break; }
                catch { err++; }
            }

            StatusLine = err > 0
                ? $"Terminado: {done} ok, {err} con error."
                : $"Terminado: {done} archivo(s) preparados en _preparado\\.";
        }
        finally
        {
            IsBusy = false;
            ProgressPercent = 0;
            _cts = null;
        }
    }
}
