using System.Collections.ObjectModel;
using System.IO;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>Extraer la pista de audio, o extraer frames a imágenes.</summary>
public sealed class ExtractViewModel : ToolViewModelBase
{
    public ObservableCollection<string> Modes { get; } = new(new[] { "Audio", "Frames" });
    public ObservableCollection<string> AudioFormats { get; } = new(new[] { "mp3", "aac", "wav", "flac", "ogg" });
    public ObservableCollection<string> FrameFormats { get; } = new(new[] { "jpg", "png" });

    public ExtractViewModel()
    {
        _mode = "Audio";
        _audioFormat = "mp3";
        _frameFormat = "jpg";
        ExtractCommand = new RelayCommand(async () => await RunAsync(), () => !string.IsNullOrEmpty(InputFile) && !IsBusy);
    }

    private string _mode;
    public string Mode
    {
        get => _mode;
        set { if (Set(ref _mode, value)) { OnPropertyChanged(nameof(IsAudio)); OnPropertyChanged(nameof(IsFrames)); } }
    }
    public bool IsAudio => Mode == "Audio";
    public bool IsFrames => Mode == "Frames";

    private string _audioFormat;
    public string AudioFormat { get => _audioFormat; set => Set(ref _audioFormat, value); }

    private string _frameFormat;
    public string FrameFormat { get => _frameFormat; set => Set(ref _frameFormat, value); }

    private double _fps = 1;
    public double Fps { get => _fps; set => Set(ref _fps, value); }

    public RelayCommand ExtractCommand { get; }

    protected override void OnInputChanged() => ExtractCommand.RaiseCanExecuteChanged();
    protected override void OnBusyChanged() => ExtractCommand.RaiseCanExecuteChanged();

    private async Task RunAsync()
    {
        Cts = new CancellationTokenSource();
        IsBusy = true;
        try
        {
            if (IsAudio)
            {
                var output = AutoOutput(InputFile, "_audio", AudioFormat);
                var args = FfmpegOps.BuildExtractAudioArgs(InputFile, output, AudioFormat, "192k");
                StatusLine = "Extrayendo audio…";
                await Ffmpeg.RunAsync(args, output, 0, Cts.Token);
                StatusLine = $"Hecho → {Path.GetFileName(output)}";
            }
            else
            {
                var dir = Path.GetDirectoryName(InputFile) ?? "";
                var stem = Path.GetFileNameWithoutExtension(InputFile);
                var outDir = Path.Combine(dir, $"{stem}_frames");
                Directory.CreateDirectory(outDir);
                var pattern = Path.Combine(outDir, $"frame_%05d.{FrameFormat}");
                var args = FfmpegOps.BuildExtractFramesArgs(InputFile, pattern, Fps, 2);
                StatusLine = "Extrayendo frames…";
                await Ffmpeg.RunAsync(args, outDir, 0, Cts.Token);
                StatusLine = $"Hecho → {stem}_frames\\";
            }
        }
        catch (OperationCanceledException) { StatusLine = "Cancelado."; }
        catch (Exception ex) { StatusLine = "Error: " + ex.Message; }
        finally { IsBusy = false; ProgressPercent = 0; Cts = null; }
    }
}
