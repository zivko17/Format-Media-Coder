using System.IO;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace FormatMediaCoder.App.Services;

/// <summary>
/// Rasteriza páginas de PDF con el motor que ya trae Windows (el mismo que usa
/// Edge para ver PDFs). No añade dependencias ni binarios: el equivalente en
/// PDF de lo que FFmpegService hace con el vídeo, y como aquel, es la única
/// parte atada a Windows — la lógica de tamaños y el .pptx viven en el Core.
/// </summary>
public sealed class PdfRenderService : IDisposable
{
    private FileStream? _file;
    private PdfDocument? _document;

    public string FilePath { get; private set; } = "";

    public uint PageCount => _document?.PageCount ?? 0;

    /// <summary>Abre el PDF. Si está protegido, hay que pasar la contraseña.</summary>
    public async Task OpenAsync(string path, string? password = null)
    {
        Close();

        if (!File.Exists(path))
            throw new FileNotFoundException($"No se encuentra el archivo: {path}", path);
        if (!LooksLikePdf(path))
            throw new InvalidDataException($"«{Path.GetFileName(path)}» no es un PDF (le falta la cabecera %PDF).");

        _file = File.OpenRead(path);
        var stream = _file.AsRandomAccessStream();

        _document = string.IsNullOrEmpty(password)
            ? await PdfDocument.LoadFromStreamAsync(stream)
            : await PdfDocument.LoadFromStreamAsync(stream, password);

        FilePath = path;
    }

    /// <summary>
    /// Tamaño de la página tal y como lo declara el documento. De aquí solo
    /// importa la proporción: es lo que decide si la página llena la diapositiva
    /// o se centra con bandas, y a cuántos píxeles se rasteriza.
    /// </summary>
    public (double Width, double Height) PageSize(uint index)
    {
        using var page = Document.GetPage(index);
        return (page.Size.Width, page.Size.Height);
    }

    /// <summary>
    /// Rasteriza una página al tamaño exacto pedido y devuelve el PNG o el JPEG.
    /// El fondo se pinta de blanco: un PDF con transparencia sobre un fondo
    /// negro de diapositiva dejaría el texto negro invisible.
    /// </summary>
    public async Task<byte[]> RenderPageAsync(uint index, int pixelWidth, int pixelHeight, bool jpeg, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var page = Document.GetPage(index);
        var options = new PdfPageRenderOptions
        {
            DestinationWidth = (uint)Math.Max(1, pixelWidth),
            DestinationHeight = (uint)Math.Max(1, pixelHeight),
            BitmapEncoderId = jpeg ? BitmapEncoder.JpegEncoderId : BitmapEncoder.PngEncoderId,
            BackgroundColor = new Windows.UI.Color { A = 255, R = 255, G = 255, B = 255 },
        };

        using var buffer = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(buffer, options).AsTask(ct);

        var bytes = new byte[buffer.Size];
        using var reader = new DataReader(buffer.GetInputStreamAt(0));
        await reader.LoadAsync((uint)buffer.Size).AsTask(ct);
        reader.ReadBytes(bytes);
        return bytes;
    }

    private PdfDocument Document =>
        _document ?? throw new InvalidOperationException("No hay ningún PDF abierto.");

    /// <summary>Comprobación barata de cabecera para dar un error claro y no uno de WinRT.</summary>
    private static bool LooksLikePdf(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var head = new byte[1024];
            int read = fs.Read(head, 0, head.Length);
            // Algunos PDFs traen basura antes de la cabecera; los lectores lo toleran.
            return System.Text.Encoding.ASCII.GetString(head, 0, Math.Max(0, read)).Contains("%PDF-", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private void Close()
    {
        _document = null;
        _file?.Dispose();
        _file = null;
        FilePath = "";
    }

    public void Dispose() => Close();
}
