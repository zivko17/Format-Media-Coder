using FormatMediaCoder.App.Services;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>
/// Plumbing compartido por las herramientas: archivo de entrada, estado de
/// ocupado, progreso, línea de estado y cancelación. En WinUI la elección de
/// archivo se hace en la página (necesita el handle de ventana), y aquí solo se
/// recibe la ruta ya elegida vía <see cref="InputFile"/>.
/// </summary>
public abstract class ToolViewModelBase : ObservableObject
{
    protected readonly FFmpegService Ffmpeg = FFmpegService.CreateDefault();
    protected CancellationTokenSource? Cts;

    protected ToolViewModelBase()
    {
        Ffmpeg.Progress += (pct, tm) => { ProgressPercent = pct; ProgressTimemark = tm; };
        CancelCommand = new RelayCommand(() => Cts?.Cancel(), () => IsBusy);
    }

    private string _inputFile = "";
    public string InputFile
    {
        get => _inputFile;
        set { if (Set(ref _inputFile, value)) OnInputChanged(); }
    }

    protected virtual void OnInputChanged() { }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        protected set
        {
            if (Set(ref _isBusy, value))
            {
                CancelCommand.RaiseCanExecuteChanged();
                OnBusyChanged();
            }
        }
    }

    protected virtual void OnBusyChanged() { }

    private double _progressPercent;
    public double ProgressPercent { get => _progressPercent; set => Set(ref _progressPercent, value); }

    private string _progressTimemark = "";
    public string ProgressTimemark { get => _progressTimemark; set => Set(ref _progressTimemark, value); }

    private string _statusLine = "";
    public string StatusLine { get => _statusLine; protected set => Set(ref _statusLine, value); }

    public RelayCommand CancelCommand { get; }

    /// <summary>Nombre de salida junto al de entrada con un sufijo y extensión dados.</summary>
    protected static string AutoOutput(string input, string suffix, string ext)
    {
        var dir = System.IO.Path.GetDirectoryName(input) ?? "";
        var stem = System.IO.Path.GetFileNameWithoutExtension(input);
        return PathUtils.UniquePath(System.IO.Path.Combine(dir, $"{stem}{suffix}.{ext}"));
    }
}
