using System.Collections.ObjectModel;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>Convertir vídeo: cambia contenedor/codec. Incluye HAP con variante (fix del alfa).</summary>
public sealed class ConvertViewModel : ToolViewModelBase
{
    public sealed record FormatPreset(string Key, string Label, string Ext, string? VideoCodec, string AudioCodec);

    public ObservableCollection<FormatPreset> Formats { get; } = new(new[]
    {
        new FormatPreset("mp4",  "MP4 (H.264)",    "mp4", "libx264", "aac"),
        new FormatPreset("mov",  "MOV (ProRes)",   "mov", "prores",  "pcm_s16le"),
        new FormatPreset("hap",  "MOV (HAP)",      "mov", "hap",     "pcm_s16le"),
        new FormatPreset("mkv",  "MKV (copy)",     "mkv", "copy",    "copy"),
        new FormatPreset("webm", "WebM (VP9)",     "webm","libvpx-vp9","libopus"),
        new FormatPreset("mp3",  "MP3 (audio)",    "mp3", null,      "libmp3lame"),
        new FormatPreset("aac",  "AAC (audio)",    "aac", null,      "aac"),
        new FormatPreset("wav",  "WAV (audio)",    "wav", null,      "pcm_s16le"),
    });

    public ObservableCollection<string> HapVariants { get; } = new(new[] { "hap_alpha", "hap_q", "hap" });
    public ObservableCollection<string> VideoBitrates { get; } = new(new[] { "", "2000k", "4000k", "8000k", "15000k", "25000k" });
    public ObservableCollection<string> AudioBitrates { get; } = new(new[] { "", "128k", "192k", "256k", "320k" });

    public ConvertViewModel()
    {
        _selectedFormat = Formats[0];
        _selectedHapVariant = HapVariants[0];
        ConvertCommand = new RelayCommand(async () => await ConvertAsync(),
            () => !string.IsNullOrEmpty(InputFile) && !IsBusy);
    }

    private FormatPreset _selectedFormat;
    public FormatPreset SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            if (Set(ref _selectedFormat, value))
            {
                OnPropertyChanged(nameof(ShowHapVariant));
                OnPropertyChanged(nameof(ShowBitrates));
            }
        }
    }

    private string _selectedHapVariant;
    public string SelectedHapVariant { get => _selectedHapVariant; set => Set(ref _selectedHapVariant, value); }

    private string _videoBitrate = "";
    public string VideoBitrate { get => _videoBitrate; set => Set(ref _videoBitrate, value); }

    private string _audioBitrate = "";
    public string AudioBitrate { get => _audioBitrate; set => Set(ref _audioBitrate, value); }

    private bool _normalizeAudio;
    public bool NormalizeAudio { get => _normalizeAudio; set => Set(ref _normalizeAudio, value); }

    public bool ShowHapVariant => SelectedFormat.Key == "hap";
    public bool ShowBitrates => SelectedFormat.VideoCodec is not (null or "prores" or "hap" or "copy");

    public RelayCommand ConvertCommand { get; }

    protected override void OnInputChanged() => ConvertCommand.RaiseCanExecuteChanged();
    protected override void OnBusyChanged() => ConvertCommand.RaiseCanExecuteChanged();

    private async Task ConvertAsync()
    {
        Cts = new CancellationTokenSource();
        IsBusy = true;
        var output = AutoOutput(InputFile, "_out", SelectedFormat.Ext);
        try
        {
            double dur = 0;
            try { dur = (await Ffmpeg.ProbeAsync(InputFile, Cts.Token)).DurationSeconds; } catch { }

            var args = FfmpegOps.BuildConvertArgs(new FfmpegOps.ConvertOptions
            {
                Input = InputFile,
                Output = output,
                VideoCodec = SelectedFormat.VideoCodec,
                HapVariant = ShowHapVariant ? SelectedHapVariant : null,
                AudioCodec = SelectedFormat.AudioCodec,
                VideoBitrate = ShowBitrates ? VideoBitrate : null,
                AudioBitrate = AudioBitrate,
                NormalizeAudio = NormalizeAudio,
            });

            StatusLine = "Convirtiendo…";
            await Ffmpeg.RunAsync(args, output, dur, Cts.Token);
            StatusLine = $"Hecho → {System.IO.Path.GetFileName(output)}";
        }
        catch (OperationCanceledException) { StatusLine = "Cancelado."; }
        catch (Exception ex) { StatusLine = "Error: " + ex.Message; }
        finally { IsBusy = false; ProgressPercent = 0; Cts = null; }
    }
}
