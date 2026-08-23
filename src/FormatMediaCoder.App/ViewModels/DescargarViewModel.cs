using System.Collections.ObjectModel;
using FormatMediaCoder.App.Services;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>Descargar vídeo/audio de una URL con yt-dlp.</summary>
public sealed class DescargarViewModel : ObservableObject
{
    private readonly YtDlpService _ytdlp = YtDlpService.CreateDefault();
    private CancellationTokenSource? _cts;

    public sealed record KindOption(string Label, YtDlpArgs.Kind Kind);

    public ObservableCollection<KindOption> Kinds { get; } = new(new[]
    {
        new KindOption("Vídeo (MP4)", YtDlpArgs.Kind.Video),
        new KindOption("Audio (MP3)", YtDlpArgs.Kind.AudioMp3),
        new KindOption("Audio (mejor calidad)", YtDlpArgs.Kind.AudioBest),
    });

    public DescargarViewModel()
    {
        _selectedKind = Kinds[0];
        _outputDir = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        _ytdlp.Progress += (pct, line) =>
        {
            if (pct >= 0) ProgressPercent = pct;
            StatusLine = line;
        };
        DownloadCommand = new RelayCommand(async () => await RunAsync(),
            () => !string.IsNullOrWhiteSpace(Url) && !IsBusy);
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
    }

    private string _url = "";
    public string Url
    {
        get => _url;
        set { if (Set(ref _url, value)) DownloadCommand.RaiseCanExecuteChanged(); }
    }

    private KindOption _selectedKind;
    public KindOption SelectedKind { get => _selectedKind; set => Set(ref _selectedKind, value); }

    private string _outputDir;
    public string OutputDir { get => _outputDir; set => Set(ref _outputDir, value); }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set { if (Set(ref _isBusy, value)) { DownloadCommand.RaiseCanExecuteChanged(); CancelCommand.RaiseCanExecuteChanged(); } }
    }

    private double _progressPercent;
    public double ProgressPercent { get => _progressPercent; set => Set(ref _progressPercent, value); }

    private string _statusLine = "";
    public string StatusLine { get => _statusLine; private set => Set(ref _statusLine, value); }

    public RelayCommand DownloadCommand { get; }
    public RelayCommand CancelCommand { get; }

    private async Task RunAsync()
    {
        _cts = new CancellationTokenSource();
        IsBusy = true;
        ProgressPercent = 0;
        try
        {
            StatusLine = "Descargando…";
            await _ytdlp.DownloadAsync(Url.Trim(), OutputDir, SelectedKind.Kind, _cts.Token);
            StatusLine = $"Descarga completada en {OutputDir}";
            ProgressPercent = 100;
        }
        catch (OperationCanceledException) { StatusLine = "Cancelado."; _ytdlp.Cancel(); }
        catch (Exception ex) { StatusLine = "Error: " + ex.Message; }
        finally { IsBusy = false; _cts = null; }
    }
}
