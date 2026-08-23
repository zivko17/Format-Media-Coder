using System.Collections.ObjectModel;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>Comprimir vídeo con libx264 (CRF + preset + resolución).</summary>
public sealed class CompressViewModel : ToolViewModelBase
{
    public ObservableCollection<string> Presets { get; } = new(new[]
    { "ultrafast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow" });

    public ObservableCollection<string> Resolutions { get; } = new(new[]
    { "original", "1920x1080", "1280x720", "854x480", "640x360" });

    public sealed record Quality(string Label, int Crf);

    public ObservableCollection<Quality> Qualities { get; } = new(new[]
    {
        new Quality("Máxima calidad (más grande)", 18),
        new Quality("Alta", 20),
        new Quality("Media (recomendado)", 23),
        new Quality("Baja (más pequeño)", 26),
        new Quality("Mínima (muy pequeño)", 30),
    });

    public CompressViewModel()
    {
        _preset = "medium";
        _resolution = "original";
        _selectedQuality = Qualities[2]; // Media
        CompressCommand = new RelayCommand(async () => await RunAsync(), () => !string.IsNullOrEmpty(InputFile) && !IsBusy);
    }

    private Quality _selectedQuality;
    public Quality SelectedQuality { get => _selectedQuality; set => Set(ref _selectedQuality, value); }

    private string _preset;
    public string Preset { get => _preset; set => Set(ref _preset, value); }

    private string _resolution;
    public string Resolution { get => _resolution; set => Set(ref _resolution, value); }

    private bool _removeAudio;
    public bool RemoveAudio { get => _removeAudio; set => Set(ref _removeAudio, value); }

    public RelayCommand CompressCommand { get; }

    protected override void OnInputChanged() => CompressCommand.RaiseCanExecuteChanged();
    protected override void OnBusyChanged() => CompressCommand.RaiseCanExecuteChanged();

    private async Task RunAsync()
    {
        Cts = new CancellationTokenSource();
        IsBusy = true;
        var output = AutoOutput(InputFile, "_comp", "mp4");
        try
        {
            double dur = 0;
            try { dur = (await Ffmpeg.ProbeAsync(InputFile, Cts.Token)).DurationSeconds; } catch { }

            var args = FfmpegOps.BuildCompressArgs(new FfmpegOps.CompressOptions
            {
                Input = InputFile, Output = output,
                Crf = SelectedQuality.Crf, Preset = Preset,
                Resolution = Resolution, RemoveAudio = RemoveAudio,
                AudioBitrate = "192k",
            });
            StatusLine = "Comprimiendo…";
            await Ffmpeg.RunAsync(args, output, dur, Cts.Token);
            StatusLine = $"Hecho → {System.IO.Path.GetFileName(output)}";
        }
        catch (OperationCanceledException) { StatusLine = "Cancelado."; }
        catch (Exception ex) { StatusLine = "Error: " + ex.Message; }
        finally { IsBusy = false; ProgressPercent = 0; Cts = null; }
    }
}
