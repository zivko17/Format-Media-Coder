using System.Collections.ObjectModel;
using FormatMediaCoder.Core.Services;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>Convertir imágenes (JPG/PNG/WEBP/ICO).</summary>
public sealed class ImageConvertViewModel : ToolViewModelBase
{
    public ObservableCollection<string> Formats { get; } = new(new[] { "jpg", "png", "webp", "ico" });

    public ImageConvertViewModel()
    {
        _format = "png";
        ConvertCommand = new RelayCommand(async () => await RunAsync(), () => !string.IsNullOrEmpty(InputFile) && !IsBusy);
    }

    private string _format;
    public string Format { get => _format; set => Set(ref _format, value); }

    public RelayCommand ConvertCommand { get; }

    protected override void OnInputChanged() => ConvertCommand.RaiseCanExecuteChanged();
    protected override void OnBusyChanged() => ConvertCommand.RaiseCanExecuteChanged();

    private async Task RunAsync()
    {
        Cts = new CancellationTokenSource();
        IsBusy = true;
        var output = AutoOutput(InputFile, "_out", Format);
        try
        {
            var args = FfmpegOps.BuildImageConvertArgs(InputFile, output, Format);
            StatusLine = "Convirtiendo…";
            await Ffmpeg.RunAsync(args, output, 0, Cts.Token);
            StatusLine = $"Hecho → {System.IO.Path.GetFileName(output)}";
        }
        catch (OperationCanceledException) { StatusLine = "Cancelado."; }
        catch (Exception ex) { StatusLine = "Error: " + ex.Message; }
        finally { IsBusy = false; ProgressPercent = 0; Cts = null; }
    }
}
