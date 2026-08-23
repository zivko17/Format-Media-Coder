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

    public CompressViewModel()
    {
        _preset = "medium";
        _resolution = "original";
        CompressCommand = new RelayCommand(async () => await RunAsync(), () => !string.IsNullOrEmpty(InputFile) && !IsBusy);
    }

    private double _crf = 23;
    public double Crf { get => _crf; set => Set(ref _crf, value); }

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
                Crf = (int)Crf, Preset = Preset,
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
