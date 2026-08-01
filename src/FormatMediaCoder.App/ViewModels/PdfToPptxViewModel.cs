using System.Collections.ObjectModel;
using System.IO;
using FormatMediaCoder.App.Services;
using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Services;
using Microsoft.Win32;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>
/// PDF → PowerPoint: el cliente manda el guion del evento en PDF y el bolo se
/// dispara desde PowerPoint. Cada página se convierte en una diapositiva a
/// página completa, con su proporción intacta.
///
/// No intenta recuperar el texto como cajas editables: eso siempre sale mal
/// (fuentes que no están en el equipo, líneas que se descolocan) y en un evento
/// lo que no puede pasar es que una diapositiva se vea distinta a como la
/// aprobó el cliente. Se rasteriza: lo que se ve es exactamente el PDF.
/// </summary>
public sealed class PdfToPptxViewModel : ObservableObject
{
    public sealed record SizeOption(SlideSizeMode Mode, string Label);
    public sealed record ResolutionOption(int LongEdgePixels, string Label);
    public sealed record FormatOption(SlideImageFormat Format, string Label);
    public sealed record BackgroundOption(string Hex, string Label);

    public ObservableCollection<string> Files { get; } = new();

    public ObservableCollection<SizeOption> Sizes { get; } = new(new[]
    {
        new SizeOption(SlideSizeMode.Widescreen16x9, "16:9 — panorámica (lo normal en proyección)"),
        new SizeOption(SlideSizeMode.MatchSource, "Como el PDF — conserva su formato exacto"),
        new SizeOption(SlideSizeMode.Standard4x3, "4:3 — material y proyectores antiguos"),
    });

    public ObservableCollection<ResolutionOption> Resolutions { get; } = new(new[]
    {
        new ResolutionOption(1920, "Full HD — 1920 px (proyección normal)"),
        new ResolutionOption(2560, "2K — 2560 px"),
        new ResolutionOption(3840, "4K — 3840 px (pantallas LED grandes)"),
        new ResolutionOption(1280, "Ligera — 1280 px (para enviar por correo)"),
    });

    public ObservableCollection<FormatOption> Formats { get; } = new(new[]
    {
        new FormatOption(SlideImageFormat.Png, "PNG — texto y vectores nítidos"),
        new FormatOption(SlideImageFormat.Jpeg, "JPEG — archivo mucho más ligero (escaneados y fotos)"),
    });

    public ObservableCollection<BackgroundOption> Backgrounds { get; } = new(new[]
    {
        new BackgroundOption("000000", "Negro — no se nota en pantalla"),
        new BackgroundOption("FFFFFF", "Blanco"),
    });

    public PdfToPptxViewModel()
    {
        _selectedSize = Sizes[0];
        _selectedResolution = Resolutions[0];
        _selectedFormat = Formats[0];
        _selectedBackground = Backgrounds[0];

        AddFilesCommand = new RelayCommand(AddFiles, () => !IsBusy);
        RemoveCommand = new RelayCommand(p => Remove(p as string), p => !IsBusy && p is string);
        MoveUpCommand = new RelayCommand(p => Move(p as string, -1), p => !IsBusy && p is string);
        MoveDownCommand = new RelayCommand(p => Move(p as string, +1), p => !IsBusy && p is string);
        ClearCommand = new RelayCommand(Files.Clear, () => !IsBusy && Files.Count > 0);
        SaveAsCommand = new RelayCommand(SaveAs, () => !IsBusy);
        ConvertCommand = new RelayCommand(async () => await ConvertAsync(), () => !IsBusy && Files.Count > 0);
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);

        Files.CollectionChanged += (_, _) => OnFilesChanged();
    }

    // ── Estado ──────────────────────────────────────────────────────────────
    private CancellationTokenSource? _cts;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!Set(ref _isBusy, value)) return;
            foreach (var c in new[] { AddFilesCommand, RemoveCommand, MoveUpCommand, MoveDownCommand, ClearCommand, SaveAsCommand, ConvertCommand, CancelCommand })
                c.RaiseCanExecuteChanged();
        }
    }

    private double _progressPercent;
    public double ProgressPercent { get => _progressPercent; private set => Set(ref _progressPercent, value); }

    private string _statusLine = "";
    public string StatusLine { get => _statusLine; private set => Set(ref _statusLine, value); }

    private string _outputFile = "";
    public string OutputFile { get => _outputFile; set => Set(ref _outputFile, value); }

    private SizeOption _selectedSize;
    public SizeOption SelectedSize { get => _selectedSize; set => Set(ref _selectedSize, value); }

    private ResolutionOption _selectedResolution;
    public ResolutionOption SelectedResolution { get => _selectedResolution; set => Set(ref _selectedResolution, value); }

    private FormatOption _selectedFormat;
    public FormatOption SelectedFormat { get => _selectedFormat; set => Set(ref _selectedFormat, value); }

    private BackgroundOption _selectedBackground;
    public BackgroundOption SelectedBackground { get => _selectedBackground; set => Set(ref _selectedBackground, value); }

    public bool HasFiles => Files.Count > 0;

    public string FilesSummary => Files.Count switch
    {
        0 => "Ningún PDF todavía.",
        1 => "1 PDF · todas sus páginas irán a la presentación, en orden.",
        _ => $"{Files.Count} PDFs · se unen en una sola presentación, en este orden.",
    };

    // ── Comandos ────────────────────────────────────────────────────────────
    public RelayCommand AddFilesCommand { get; }
    public RelayCommand RemoveCommand { get; }
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand SaveAsCommand { get; }
    public RelayCommand ConvertCommand { get; }
    public RelayCommand CancelCommand { get; }

    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Elige los PDFs",
            Filter = "PDF|*.pdf|Todos|*.*",
            Multiselect = true,
        };
        if (dialog.ShowDialog() == true) AddFiles(dialog.FileNames);
    }

    /// <summary>Alta de archivos desde el diálogo o desde arrastrar y soltar.</summary>
    public void AddFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            if (!string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase)) continue;
            if (Files.Contains(path, StringComparer.OrdinalIgnoreCase)) continue;
            Files.Add(path);
        }
    }

    private void Remove(string? path)
    {
        if (path is not null) Files.Remove(path);
    }

    private void Move(string? path, int delta)
    {
        if (path is null) return;
        int i = Files.IndexOf(path);
        int j = i + delta;
        if (i < 0 || j < 0 || j >= Files.Count) return;
        Files.Move(i, j);
    }

    private void OnFilesChanged()
    {
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(FilesSummary));
        ClearCommand.RaiseCanExecuteChanged();
        ConvertCommand.RaiseCanExecuteChanged();
        if (Files.Count > 0 && string.IsNullOrEmpty(OutputFile)) OutputFile = DefaultOutput();
        if (Files.Count == 0) StatusLine = "";
    }

    private string DefaultOutput()
    {
        var first = Files[0];
        var dir = Path.GetDirectoryName(first) ?? "";
        var stem = Files.Count == 1 ? Path.GetFileNameWithoutExtension(first) : "presentacion";
        return PathUtils.UniquePath(Path.Combine(dir, stem + ".pptx"));
    }

    private void SaveAs()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar la presentación",
            Filter = "Presentación de PowerPoint|*.pptx",
            DefaultExt = "pptx",
            FileName = Path.GetFileName(string.IsNullOrEmpty(OutputFile) ? "presentacion.pptx" : OutputFile),
            // Nunca se sobrescribe: si el nombre ya existe, se guarda como « (2)».
            // Preguntar por reemplazar sería mentir sobre lo que hace la app.
            OverwritePrompt = false,
        };
        if (dialog.ShowDialog() == true) OutputFile = dialog.FileName;
    }

    // ── Conversión ──────────────────────────────────────────────────────────
    private async Task ConvertAsync()
    {
        var documents = new List<PdfRenderService>();
        string output = string.IsNullOrWhiteSpace(OutputFile) ? DefaultOutput() : PathUtils.UniquePath(OutputFile);

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        IsBusy = true;
        ProgressPercent = 0;

        try
        {
            // 1) Abrir todos los PDFs primero: así se sabe cuántas diapositivas
            //    habrá antes de escribir nada, y un PDF ilegible se detecta antes
            //    de dejar medio archivo en el disco.
            int totalPages = 0;
            foreach (var path in Files)
            {
                var doc = new PdfRenderService();
                try
                {
                    await doc.OpenAsync(path);
                }
                catch (Exception ex)
                {
                    doc.Dispose();
                    throw new InvalidOperationException($"No se puede leer «{Path.GetFileName(path)}»: {Describe(ex)}", ex);
                }

                if (doc.PageCount == 0)
                {
                    doc.Dispose();
                    throw new InvalidOperationException($"«{Path.GetFileName(path)}» no tiene páginas.");
                }

                documents.Add(doc);
                totalPages += (int)doc.PageCount;
            }

            var (firstWidth, firstHeight) = documents[0].PageSize(0);
            var slideSize = SlideLayout.Resolve(SelectedSize.Mode, firstWidth, firstHeight);
            bool jpeg = SelectedFormat.Format == SlideImageFormat.Jpeg;

            StatusLine = $"{totalPages} páginas · {slideSize.InchesLabel}. Convirtiendo…";

            // 2) Volcar página a página. El escritor va soltando cada diapositiva
            //    al disco, así que un PDF de 300 páginas en 4K no se acumula en RAM.
            using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new PptxPackageWriter(file, totalPages, new PptxOptions
            {
                Size = slideSize,
                Title = Path.GetFileNameWithoutExtension(output),
                BackgroundHex = SelectedBackground.Hex,
            }))
            {
                int done = 0;
                foreach (var doc in documents)
                {
                    string name = Path.GetFileName(doc.FilePath);
                    for (uint page = 0; page < doc.PageCount; page++)
                    {
                        ct.ThrowIfCancellationRequested();

                        var (pw, ph) = doc.PageSize(page);
                        var (px, py) = SlideLayout.RenderPixels(pw, ph, SelectedResolution.LongEdgePixels);
                        var image = await doc.RenderPageAsync(page, px, py, jpeg, ct);

                        writer.AddSlide(new PptxSlide
                        {
                            Image = image,
                            Format = SelectedFormat.Format,
                            PixelWidth = px,
                            PixelHeight = py,
                            Name = $"{name} — página {page + 1}",
                        });

                        done++;
                        ProgressPercent = done * 100.0 / totalPages;
                        StatusLine = $"Página {done} de {totalPages}…";
                    }
                }

                writer.Complete();
            }

            OutputFile = output;
            StatusLine = $"Hecho → {Path.GetFileName(output)} · {totalPages} diapositivas · {Size(new FileInfo(output).Length)}";
        }
        catch (OperationCanceledException)
        {
            Discard(output);
            StatusLine = "Cancelado. No se ha dejado ningún archivo a medias.";
        }
        catch (Exception ex)
        {
            Discard(output);
            StatusLine = "Error: " + Describe(ex);
        }
        finally
        {
            foreach (var doc in documents) doc.Dispose();
            IsBusy = false;
            ProgressPercent = 0;
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>Un .pptx a medias no sirve para nada: se borra (regla del brief §6.3).</summary>
    private static void Discard(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* si está bloqueado, se queda */ }
    }

    private static string Describe(Exception ex) => ex switch
    {
        InvalidDataException => ex.Message,
        InvalidOperationException => ex.Message,
        FileNotFoundException => ex.Message,
        UnauthorizedAccessException => "Windows no deja escribir ahí. Prueba a guardarlo en otra carpeta.",
        _ when ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
            => "El PDF está protegido con contraseña. Ábrelo y guárdalo sin protección.",
        _ => ex.Message,
    };

    private static string Size(long bytes) => bytes switch
    {
        >= 1024L * 1024 * 1024 => $"{bytes / 1024d / 1024 / 1024:0.#} GB",
        >= 1024 * 1024 => $"{bytes / 1024d / 1024:0.#} MB",
        _ => $"{bytes / 1024d:0} KB",
    };
}
