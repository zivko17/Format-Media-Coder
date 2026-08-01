using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using FormatMediaCoder.Core.Models;

namespace FormatMediaCoder.Core.Services;

/// <summary>Formato en el que viaja la imagen de cada diapositiva.</summary>
public enum SlideImageFormat
{
    /// <summary>Sin pérdidas. Texto y vectores nítidos; archivos grandes.</summary>
    Png,
    /// <summary>Con pérdidas. Para PDFs escaneados o llenos de fotos; mucho más ligero.</summary>
    Jpeg,
}

/// <summary>Una diapositiva: una imagen a página completa y su nombre.</summary>
public sealed class PptxSlide
{
    public byte[] Image { get; init; } = Array.Empty<byte>();
    public SlideImageFormat Format { get; init; } = SlideImageFormat.Png;
    public int PixelWidth { get; init; }
    public int PixelHeight { get; init; }
    /// <summary>Nombre visible del objeto en PowerPoint, p. ej. «dossier.pdf — página 3».</summary>
    public string Name { get; init; } = "";
}

/// <summary>Ajustes de la presentación completa.</summary>
public sealed class PptxOptions
{
    public SlideSize Size { get; init; } = SlideSize.Widescreen;
    public string Title { get; init; } = "";
    /// <summary>Color de fondo en hexadecimal RRGGBB: lo que se ve en las bandas.</summary>
    public string BackgroundHex { get; init; } = "000000";
    /// <summary>Fecha que se escribe en las propiedades del archivo (inyectable para tests).</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Escribe un .pptx (OOXML sobre ZIP) a mano, una diapositiva cada vez.
///
/// Se escribe a mano y no con una librería por dos razones: el Core no tiene
/// dependencias (se compila y se testea en cualquier sistema) y, sobre todo,
/// las diapositivas se van volcando según se generan, así que un PDF de 300
/// páginas en 4K no tiene que caber entero en memoria.
///
/// El número de diapositivas se pide por adelantado a propósito: permite escribir
/// [Content_Types].xml y presentation.xml al principio del ZIP, en el mismo orden
/// que un archivo salido de PowerPoint, que es el orden que menos sorpresas da.
/// </summary>
public sealed class PptxPackageWriter : IDisposable
{
    private const string NsRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private readonly ZipArchive _zip;
    private readonly PptxOptions _options;
    private readonly int _slideCount;
    private int _added;
    private bool _completed;

    public PptxPackageWriter(Stream output, int slideCount, PptxOptions? options = null, bool leaveOpen = false)
    {
        if (slideCount < 1) throw new ArgumentOutOfRangeException(nameof(slideCount), "Una presentación necesita al menos una diapositiva.");

        _slideCount = slideCount;
        _options = options ?? new PptxOptions();
        _zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen);

        WriteFixedParts();
    }

    /// <summary>Añade la siguiente diapositiva. El orden de llamada es el orden del pase.</summary>
    public void AddSlide(PptxSlide slide)
    {
        ArgumentNullException.ThrowIfNull(slide);
        if (_completed) throw new InvalidOperationException("La presentación ya está cerrada.");
        if (_added >= _slideCount) throw new InvalidOperationException($"Se anunciaron {_slideCount} diapositivas y se está añadiendo una más.");

        int n = ++_added;
        string ext = slide.Format == SlideImageFormat.Jpeg ? "jpeg" : "png";

        // La imagen ya viene comprimida (PNG/JPEG): volver a comprimirla solo gasta CPU.
        AddBinary($"ppt/media/image{n}.{ext}", slide.Image, CompressionLevel.NoCompression);

        var place = SlideLayout.Fit(slide.PixelWidth, slide.PixelHeight, _options.Size);
        AddXml($"ppt/slides/slide{n}.xml", SlideXml(slide, place, n));
        AddXml($"ppt/slides/_rels/slide{n}.xml.rels", SlideRelsXml(n, ext));
    }

    /// <summary>Cierra el paquete. Si faltan diapositivas, falla en vez de dejar un archivo cojo.</summary>
    public void Complete()
    {
        if (_completed) return;
        if (_added != _slideCount)
            throw new InvalidOperationException($"Se anunciaron {_slideCount} diapositivas y se escribieron {_added}.");
        _completed = true;
    }

    public void Dispose() => _zip.Dispose();

    // ── Atajo de una línea para casos sencillos y para los tests ─────────────
    public static void WriteFile(string path, IReadOnlyList<PptxSlide> slides, PptxOptions? options = null)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        Write(fs, slides, options);
    }

    public static void Write(Stream output, IReadOnlyList<PptxSlide> slides, PptxOptions? options = null)
    {
        using var writer = new PptxPackageWriter(output, slides.Count, options, leaveOpen: true);
        foreach (var s in slides) writer.AddSlide(s);
        writer.Complete();
    }

    // ── Partes fijas del paquete ────────────────────────────────────────────
    private void WriteFixedParts()
    {
        AddXml("[Content_Types].xml", ContentTypesXml());
        AddXml("_rels/.rels", RootRelsXml());
        AddXml("docProps/core.xml", CoreXml());
        AddXml("docProps/app.xml", AppXml());
        AddXml("ppt/presentation.xml", PresentationXml());
        AddXml("ppt/_rels/presentation.xml.rels", PresentationRelsXml());
        AddXml("ppt/slideMasters/slideMaster1.xml", SlideMasterXml());
        AddXml("ppt/slideMasters/_rels/slideMaster1.xml.rels", SlideMasterRelsXml());
        AddXml("ppt/slideLayouts/slideLayout1.xml", SlideLayoutXml());
        AddXml("ppt/slideLayouts/_rels/slideLayout1.xml.rels", SlideLayoutRelsXml());
        AddXml("ppt/theme/theme1.xml", ThemeXml());
    }

    private string ContentTypesXml()
    {
        var sb = new StringBuilder();
        sb.Append("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
            <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
            <Default Extension="xml" ContentType="application/xml"/>
            <Default Extension="png" ContentType="image/png"/>
            <Default Extension="jpeg" ContentType="image/jpeg"/>
            <Override PartName="/ppt/presentation.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"/>
            <Override PartName="/ppt/slideMasters/slideMaster1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml"/>
            <Override PartName="/ppt/slideLayouts/slideLayout1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
            <Override PartName="/ppt/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/>
            <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
            <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
            """);
        for (int i = 1; i <= _slideCount; i++)
            sb.Append($"<Override PartName=\"/ppt/slides/slide{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>");
        sb.Append("</Types>");
        return sb.ToString();
    }

    private static string RootRelsXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="ppt/presentation.xml"/>
        <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
        <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
        </Relationships>
        """;

    private string CoreXml()
    {
        string stamp = _options.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            <dc:title>{Esc(_options.Title)}</dc:title>
            <dc:creator>Format Media Coder</dc:creator>
            <cp:lastModifiedBy>Format Media Coder</cp:lastModifiedBy>
            <dcterms:created xsi:type="dcterms:W3CDTF">{stamp}</dcterms:created>
            <dcterms:modified xsi:type="dcterms:W3CDTF">{stamp}</dcterms:modified>
            </cp:coreProperties>
            """;
    }

    private string AppXml() => $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
        <PresentationFormat>Personalizado</PresentationFormat>
        <Paragraphs>0</Paragraphs>
        <Slides>{_slideCount}</Slides>
        <Notes>0</Notes>
        <HiddenSlides>0</HiddenSlides>
        <MMClips>0</MMClips>
        <ScaleCrop>false</ScaleCrop>
        <LinksUpToDate>false</LinksUpToDate>
        <SharedDoc>false</SharedDoc>
        <HyperlinksChanged>false</HyperlinksChanged>
        <Application>Format Media Coder</Application>
        <AppVersion>2.0000</AppVersion>
        </Properties>
        """;

    private string PresentationXml()
    {
        var sb = new StringBuilder();
        sb.Append("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <p:presentation xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" saveSubsetFonts="1">
            <p:sldMasterIdLst><p:sldMasterId id="2147483648" r:id="rId1"/></p:sldMasterIdLst>
            <p:sldIdLst>
            """);
        for (int i = 1; i <= _slideCount; i++)
            sb.Append($"<p:sldId id=\"{255 + i}\" r:id=\"rId{i + 1}\"/>");
        sb.Append($"""
            </p:sldIdLst>
            <p:sldSz cx="{_options.Size.WidthEmu}" cy="{_options.Size.HeightEmu}"/>
            <p:notesSz cx="6858000" cy="9144000"/>
            </p:presentation>
            """);
        return sb.ToString();
    }

    private string PresentationRelsXml()
    {
        var sb = new StringBuilder();
        sb.Append($"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
            <Relationship Id="rId1" Type="{NsRel}/slideMaster" Target="slideMasters/slideMaster1.xml"/>
            """);
        for (int i = 1; i <= _slideCount; i++)
            sb.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"{NsRel}/slide\" Target=\"slides/slide{i}.xml\"/>");
        sb.Append($"<Relationship Id=\"rId{_slideCount + 2}\" Type=\"{NsRel}/theme\" Target=\"theme/theme1.xml\"/>");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private string SlideMasterXml() => $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <p:sldMaster xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
        <p:cSld>
        <p:bg><p:bgPr><a:solidFill><a:srgbClr val="{Hex(_options.BackgroundHex)}"/></a:solidFill><a:effectLst/></p:bgPr></p:bg>
        {EmptyTree}
        </p:cSld>
        <p:clrMap bg1="lt1" tx1="dk1" bg2="lt2" tx2="dk2" accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" hlink="hlink" folHlink="folHlink"/>
        <p:sldLayoutIdLst><p:sldLayoutId id="2147483649" r:id="rId1"/></p:sldLayoutIdLst>
        </p:sldMaster>
        """;

    private static string SlideMasterRelsXml() => $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="{NsRel}/slideLayout" Target="../slideLayouts/slideLayout1.xml"/>
        <Relationship Id="rId2" Type="{NsRel}/theme" Target="../theme/theme1.xml"/>
        </Relationships>
        """;

    private static string SlideLayoutXml() => $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <p:sldLayout xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" type="blank" preserve="1">
        <p:cSld name="En blanco">
        {EmptyTree}
        </p:cSld>
        <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
        </p:sldLayout>
        """;

    private static string SlideLayoutRelsXml() => $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="{NsRel}/slideMaster" Target="../slideMasters/slideMaster1.xml"/>
        </Relationships>
        """;

    private static string SlideXml(PptxSlide slide, SlideLayout.Placement p, int index)
    {
        string name = string.IsNullOrWhiteSpace(slide.Name) ? $"Página {index}" : slide.Name;
        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
            <p:cSld>
            <p:spTree>
            <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
            <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
            <p:pic>
            <p:nvPicPr><p:cNvPr id="2" name="{Esc(name)}" descr="{Esc(name)}"/><p:cNvPicPr><a:picLocks noChangeAspect="1"/></p:cNvPicPr><p:nvPr/></p:nvPicPr>
            <p:blipFill><a:blip r:embed="rId2"/><a:stretch><a:fillRect/></a:stretch></p:blipFill>
            <p:spPr><a:xfrm><a:off x="{p.OffsetXEmu}" y="{p.OffsetYEmu}"/><a:ext cx="{p.WidthEmu}" cy="{p.HeightEmu}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr>
            </p:pic>
            </p:spTree>
            </p:cSld>
            <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
            </p:sld>
            """;
    }

    private static string SlideRelsXml(int index, string ext) => $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="{NsRel}/slideLayout" Target="../slideLayouts/slideLayout1.xml"/>
        <Relationship Id="rId2" Type="{NsRel}/image" Target="../media/image{index}.{ext}"/>
        </Relationships>
        """;

    /// <summary>Árbol de formas vacío: patrón y diseño no pintan nada, las páginas van en las diapositivas.</summary>
    private const string EmptyTree = """
        <p:spTree>
        <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
        <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
        </p:spTree>
        """;

    /// <summary>Tema mínimo pero completo: PowerPoint exige los tres estilos de cada lista.</summary>
    private static string ThemeXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Format Media Coder">
        <a:themeElements>
        <a:clrScheme name="Format Media Coder">
        <a:dk1><a:sysClr val="windowText" lastClr="000000"/></a:dk1>
        <a:lt1><a:sysClr val="window" lastClr="FFFFFF"/></a:lt1>
        <a:dk2><a:srgbClr val="1E1E1E"/></a:dk2>
        <a:lt2><a:srgbClr val="E7E6E6"/></a:lt2>
        <a:accent1><a:srgbClr val="4472C4"/></a:accent1>
        <a:accent2><a:srgbClr val="ED7D31"/></a:accent2>
        <a:accent3><a:srgbClr val="A5A5A5"/></a:accent3>
        <a:accent4><a:srgbClr val="FFC000"/></a:accent4>
        <a:accent5><a:srgbClr val="5B9BD5"/></a:accent5>
        <a:accent6><a:srgbClr val="70AD47"/></a:accent6>
        <a:hlink><a:srgbClr val="0563C1"/></a:hlink>
        <a:folHlink><a:srgbClr val="954F72"/></a:folHlink>
        </a:clrScheme>
        <a:fontScheme name="Format Media Coder">
        <a:majorFont><a:latin typeface="Calibri Light"/><a:ea typeface=""/><a:cs typeface=""/></a:majorFont>
        <a:minorFont><a:latin typeface="Calibri"/><a:ea typeface=""/><a:cs typeface=""/></a:minorFont>
        </a:fontScheme>
        <a:fmtScheme name="Format Media Coder">
        <a:fillStyleLst>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"><a:tint val="50000"/></a:schemeClr></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"><a:shade val="80000"/></a:schemeClr></a:solidFill>
        </a:fillStyleLst>
        <a:lnStyleLst>
        <a:ln w="6350" cap="flat" cmpd="sng" algn="ctr"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:prstDash val="solid"/></a:ln>
        <a:ln w="12700" cap="flat" cmpd="sng" algn="ctr"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:prstDash val="solid"/></a:ln>
        <a:ln w="19050" cap="flat" cmpd="sng" algn="ctr"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:prstDash val="solid"/></a:ln>
        </a:lnStyleLst>
        <a:effectStyleLst>
        <a:effectStyle><a:effectLst/></a:effectStyle>
        <a:effectStyle><a:effectLst/></a:effectStyle>
        <a:effectStyle><a:effectLst/></a:effectStyle>
        </a:effectStyleLst>
        <a:bgFillStyleLst>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        </a:bgFillStyleLst>
        </a:fmtScheme>
        </a:themeElements>
        <a:objectDefaults/>
        <a:extraClrSchemeLst/>
        </a:theme>
        """;

    // ── Utilidades ──────────────────────────────────────────────────────────
    private void AddXml(string entryName, string xml)
    {
        var entry = _zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var s = entry.Open();
        var bytes = new UTF8Encoding(false).GetBytes(xml);
        s.Write(bytes, 0, bytes.Length);
    }

    private void AddBinary(string entryName, byte[] data, CompressionLevel level)
    {
        var entry = _zip.CreateEntry(entryName, level);
        using var s = entry.Open();
        s.Write(data, 0, data.Length);
    }

    private static string Esc(string value) => SecurityElement.Escape(value) ?? "";

    /// <summary>Normaliza un color a RRGGBB; si no es válido, negro.</summary>
    private static string Hex(string value)
    {
        var v = (value ?? "").TrimStart('#').Trim();
        if (v.Length != 6) return "000000";
        foreach (char c in v) if (!Uri.IsHexDigit(c)) return "000000";
        return v.ToUpperInvariant();
    }
}
