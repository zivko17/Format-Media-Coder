using System.Collections.ObjectModel;
using System.IO;
using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>Edición: recortar por tiempo, unir varios clips y rotar/escalar.</summary>
public sealed class EdicionViewModel : ToolViewModelBase
{
    public ObservableCollection<string> Rotations { get; } = new(new[] { "ninguna", "cw", "ccw", "180" });
    public ObservableCollection<string> Scales { get; } = new(new[] { "original", "1920x1080", "1280x720", "854x480" });
    public ObservableCollection<string> MergeFiles { get; } = new();

    public EdicionViewModel()
    {
        _rotation = "ninguna";
        _scale = "original";
        TrimCommand = new RelayCommand(async () => await TrimAsync(), () => !string.IsNullOrEmpty(InputFile) && !IsBusy);
        FiltersCommand = new RelayCommand(async () => await FiltersAsync(), () => !string.IsNullOrEmpty(InputFile) && !IsBusy);
        MergeCommand = new RelayCommand(async () => await MergeAsync(), () => MergeFiles.Count >= 2 && !IsBusy);
    }

    // ── Recortar ──
    private string _trimStart = "00:00:00";
    public string TrimStart { get => _trimStart; set => Set(ref _trimStart, value); }
    private string _trimEnd = "00:00:10";
    public string TrimEnd { get => _trimEnd; set => Set(ref _trimEnd, value); }

    // ── Rotar / escalar ──
    private string _rotation;
    public string Rotation { get => _rotation; set => Set(ref _rotation, value); }
    private string _scale;
    public string Scale { get => _scale; set => Set(ref _scale, value); }

    public RelayCommand TrimCommand { get; }
    public RelayCommand FiltersCommand { get; }
    public RelayCommand MergeCommand { get; }

    protected override void OnInputChanged()
    {
        TrimCommand.RaiseCanExecuteChanged();
        FiltersCommand.RaiseCanExecuteChanged();
    }
    protected override void OnBusyChanged()
    {
        TrimCommand.RaiseCanExecuteChanged();
        FiltersCommand.RaiseCanExecuteChanged();
        MergeCommand.RaiseCanExecuteChanged();
    }

    public void RaiseMergeCanExecute() => MergeCommand.RaiseCanExecuteChanged();

    private async Task TrimAsync()
    {
        Cts = new CancellationTokenSource();
        IsBusy = true;
        var ext = Path.GetExtension(InputFile).TrimStart('.');
        var output = AutoOutput(InputFile, "_trim", string.IsNullOrEmpty(ext) ? "mp4" : ext);
        try
        {
            var args = FfmpegOps.BuildTrimArgs(InputFile, output, TrimStart, TrimEnd);
            StatusLine = "Recortando…";
            await Ffmpeg.RunAsync(args, output, 0, Cts.Token);
            StatusLine = $"Hecho → {Path.GetFileName(output)}";
        }
        catch (OperationCanceledException) { StatusLine = "Cancelado."; }
        catch (Exception ex) { StatusLine = "Error: " + ex.Message; }
        finally { IsBusy = false; ProgressPercent = 0; Cts = null; }
    }

    private async Task FiltersAsync()
    {
        Cts = new CancellationTokenSource();
        IsBusy = true;
        var ext = Path.GetExtension(InputFile).TrimStart('.');
        var output = AutoOutput(InputFile, "_edit", string.IsNullOrEmpty(ext) ? "mp4" : ext);
        try
        {
            Resolution? scale = null;
            if (Scale != "original")
            {
                var p = Scale.Split('x');
                if (p.Length == 2 && int.TryParse(p[0], out var w) && int.TryParse(p[1], out var h))
                    scale = new Resolution(w, h);
            }
            var rot = Rotation == "ninguna" ? null : Rotation;
            double dur = 0;
            try { dur = (await Ffmpeg.ProbeAsync(InputFile, Cts.Token)).DurationSeconds; } catch { }

            var args = FfmpegOps.BuildFiltersArgs(InputFile, output, scale, rot);
            StatusLine = "Aplicando…";
            await Ffmpeg.RunAsync(args, output, dur, Cts.Token);
            StatusLine = $"Hecho → {Path.GetFileName(output)}";
        }
        catch (OperationCanceledException) { StatusLine = "Cancelado."; }
        catch (Exception ex) { StatusLine = "Error: " + ex.Message; }
        finally { IsBusy = false; ProgressPercent = 0; Cts = null; }
    }

    private async Task MergeAsync()
    {
        if (MergeFiles.Count < 2) return;
        Cts = new CancellationTokenSource();
        IsBusy = true;
        var first = MergeFiles[0];
        var output = AutoOutput(first, "_unido", Path.GetExtension(first).TrimStart('.'));
        try
        {
            StatusLine = $"Uniendo {MergeFiles.Count} clips…";
            await Ffmpeg.MergeAsync(MergeFiles.ToList(), output, Cts.Token);
            StatusLine = $"Hecho → {Path.GetFileName(output)}";
        }
        catch (OperationCanceledException) { StatusLine = "Cancelado."; }
        catch (Exception ex) { StatusLine = "Error: " + ex.Message + " (los clips deben compartir codec/resolución para unir sin recodificar)"; }
        finally { IsBusy = false; ProgressPercent = 0; Cts = null; }
    }
}
