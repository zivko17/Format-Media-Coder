using System.Collections.ObjectModel;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>Convertir audio (MP3/AAC/FLAC/WAV/OGG).</summary>
public sealed class AudioConvertViewModel : ToolViewModelBase
{
    public sealed record AudioFormat(string Key, string Codec, bool Lossless);

    public ObservableCollection<AudioFormat> Formats { get; } = new(new[]
    {
        new AudioFormat("mp3", "libmp3lame", false),
        new AudioFormat("aac", "aac", false),
        new AudioFormat("ogg", "libvorbis", false),
        new AudioFormat("flac", "flac", true),
        new AudioFormat("wav", "pcm_s16le", true),
    });

    public ObservableCollection<string> Bitrates { get; } = new(new[] { "128k", "192k", "256k", "320k" });
    public ObservableCollection<string> SampleRates { get; } = new(new[] { "", "44100", "48000", "96000" });

    public AudioConvertViewModel()
    {
        _selectedFormat = Formats[0];
        _bitrate = "192k";
        ConvertCommand = new RelayCommand(async () => await RunAsync(), () => !string.IsNullOrEmpty(InputFile) && !IsBusy);
    }

    private AudioFormat _selectedFormat;
    public AudioFormat SelectedFormat
    {
        get => _selectedFormat;
        set { if (Set(ref _selectedFormat, value)) OnPropertyChanged(nameof(ShowBitrate)); }
    }

    private string _bitrate;
    public string Bitrate { get => _bitrate; set => Set(ref _bitrate, value); }

    private string _sampleRate = "";
    public string SampleRate { get => _sampleRate; set => Set(ref _sampleRate, value); }

    private bool _mono;
    public bool Mono { get => _mono; set => Set(ref _mono, value); }

    private bool _normalize;
    public bool Normalize { get => _normalize; set => Set(ref _normalize, value); }

    public bool ShowBitrate => !SelectedFormat.Lossless;

    public RelayCommand ConvertCommand { get; }

    protected override void OnInputChanged() => ConvertCommand.RaiseCanExecuteChanged();
    protected override void OnBusyChanged() => ConvertCommand.RaiseCanExecuteChanged();

    private async Task RunAsync()
    {
        Cts = new CancellationTokenSource();
        IsBusy = true;
        var output = AutoOutput(InputFile, "_audio", SelectedFormat.Key);
        try
        {
            double dur = 0;
            try { dur = (await Ffmpeg.ProbeAsync(InputFile, Cts.Token)).DurationSeconds; } catch { }

            var args = FfmpegOps.BuildAudioConvertArgs(new FfmpegOps.AudioConvertOptions
            {
                Input = InputFile, Output = output, Codec = SelectedFormat.Codec,
                Bitrate = SelectedFormat.Lossless ? null : Bitrate,
                SampleRate = int.TryParse(SampleRate, out var sr) ? sr : null,
                Mono = Mono, Normalize = Normalize,
            });
            StatusLine = "Convirtiendo audio…";
            await Ffmpeg.RunAsync(args, output, dur, Cts.Token);
            StatusLine = $"Hecho → {System.IO.Path.GetFileName(output)}";
        }
        catch (OperationCanceledException) { StatusLine = "Cancelado."; }
        catch (Exception ex) { StatusLine = "Error: " + ex.Message; }
        finally { IsBusy = false; ProgressPercent = 0; Cts = null; }
    }
}
