using System.IO.Compression;
using System.Xml.Linq;
using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Services;
using Xunit;

namespace FormatMediaCoder.Core.Tests;

/// <summary>
/// El .pptx se escribe a mano, así que lo que hay que demostrar es que el paquete
/// sale bien formado: las partes que exige OOXML, las relaciones apuntando a algo
/// que existe y las páginas colocadas donde toca.
/// </summary>
public class PptxWriterTests
{
    // PNG de 1×1 real, suficiente para que el paquete lleve una imagen de verdad.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static readonly XNamespace P = "http://schemas.openxmlformats.org/presentationml/2006/main";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/package/2006/relationships";

    private static PptxSlide Slide(int w = 1920, int h = 1080, string name = "") =>
        new() { Image = Png, Format = SlideImageFormat.Png, PixelWidth = w, PixelHeight = h, Name = name };

    private static ZipArchive Build(int count, PptxOptions? options = null, params PptxSlide[] slides)
    {
        var ms = new MemoryStream();
        using (var w = new PptxPackageWriter(ms, count, options, leaveOpen: true))
        {
            foreach (var s in slides) w.AddSlide(s);
            w.Complete();
        }
        ms.Position = 0;
        return new ZipArchive(ms, ZipArchiveMode.Read);
    }

    private static XDocument Xml(ZipArchive zip, string entry)
    {
        using var s = zip.GetEntry(entry)!.Open();
        return XDocument.Load(s);
    }

    [Fact]
    public void Package_HasEveryPartOoxmlRequires()
    {
        using var zip = Build(2, null, Slide(), Slide());
        var names = zip.Entries.Select(e => e.FullName).ToHashSet();

        foreach (var required in new[]
        {
            "[Content_Types].xml", "_rels/.rels", "docProps/core.xml", "docProps/app.xml",
            "ppt/presentation.xml", "ppt/_rels/presentation.xml.rels",
            "ppt/slideMasters/slideMaster1.xml", "ppt/slideMasters/_rels/slideMaster1.xml.rels",
            "ppt/slideLayouts/slideLayout1.xml", "ppt/slideLayouts/_rels/slideLayout1.xml.rels",
            "ppt/theme/theme1.xml",
            "ppt/slides/slide1.xml", "ppt/slides/_rels/slide1.xml.rels", "ppt/media/image1.png",
            "ppt/slides/slide2.xml", "ppt/slides/_rels/slide2.xml.rels", "ppt/media/image2.png",
        })
            Assert.Contains(required, names);
    }

    [Fact]
    public void ContentTypes_IsFirstEntry_AndDeclaresEverySlide()
    {
        using var zip = Build(3, null, Slide(), Slide(), Slide());

        // Los lectores de OPC esperan encontrarlo al principio del ZIP.
        Assert.Equal("[Content_Types].xml", zip.Entries[0].FullName);

        var xml = Xml(zip, "[Content_Types].xml").ToString();
        for (int i = 1; i <= 3; i++)
            Assert.Contains($"/ppt/slides/slide{i}.xml", xml);
    }

    [Fact]
    public void EveryRelationshipTargetExists()
    {
        using var zip = Build(2, null, Slide(), Slide());
        var names = zip.Entries.Select(e => e.FullName).ToHashSet();

        foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".rels")).ToList())
        {
            // Carpeta del documento dueño de este .rels: "ppt/slides/_rels/slide1.xml.rels" → "ppt/slides".
            var relsDir = Path.GetDirectoryName(entry.FullName)!.Replace('\\', '/');
            var ownerDir = relsDir[..Math.Max(0, relsDir.LastIndexOf("_rels", StringComparison.Ordinal) - 1)];

            using var s = entry.Open();
            foreach (var target in XDocument.Load(s).Descendants(R + "Relationship")
                                            .Select(r => (string)r.Attribute("Target")!))
            {
                var resolved = Normalize(string.IsNullOrEmpty(ownerDir) ? target : ownerDir + "/" + target);
                Assert.Contains(resolved, names);
            }
        }
    }

    private static string Normalize(string path)
    {
        var parts = new List<string>();
        foreach (var part in path.Split('/'))
        {
            if (part == "." || part.Length == 0) continue;
            if (part == ".." && parts.Count > 0) parts.RemoveAt(parts.Count - 1);
            else parts.Add(part);
        }
        return string.Join('/', parts);
    }

    [Fact]
    public void Presentation_CarriesSlideSizeAndOneIdPerSlide()
    {
        using var zip = Build(2, new PptxOptions { Size = SlideSize.Standard }, Slide(), Slide());
        var doc = Xml(zip, "ppt/presentation.xml");

        var size = doc.Descendants(P + "sldSz").Single();
        Assert.Equal("9144000", (string)size.Attribute("cx")!);
        Assert.Equal("6858000", (string)size.Attribute("cy")!);

        var ids = doc.Descendants(P + "sldId").ToList();
        Assert.Equal(2, ids.Count);
        Assert.Equal(new[] { "256", "257" }, ids.Select(i => (string)i.Attribute("id")!));
    }

    [Fact]
    public void Slide_PlacesPageWithTheGeometryFromSlideLayout()
    {
        using var zip = Build(1, null, Slide(1487, 2105));   // A4 vertical en 16:9
        var expected = SlideLayout.Fit(1487, 2105, SlideSize.Widescreen);

        var xfrm = Xml(zip, "ppt/slides/slide1.xml").Descendants(P + "pic").Single().Descendants(A + "xfrm").Single();
        Assert.Equal(expected.OffsetXEmu.ToString(), (string)xfrm.Element(A + "off")!.Attribute("x")!);
        Assert.Equal(expected.WidthEmu.ToString(), (string)xfrm.Element(A + "ext")!.Attribute("cx")!);
        Assert.Equal(expected.HeightEmu.ToString(), (string)xfrm.Element(A + "ext")!.Attribute("cy")!);
    }

    [Fact]
    public void Slide_NameWithMarkup_IsEscapedNotInjected()
    {
        using var zip = Build(1, null, Slide(name: "dossier <cliente> & \"final\""));
        var pic = Xml(zip, "ppt/slides/slide1.xml").Descendants(P + "cNvPr").Last();

        Assert.Equal("dossier <cliente> & \"final\"", (string)pic.Attribute("name")!);
    }

    [Fact]
    public void Jpeg_GetsItsOwnExtensionAndRelationship()
    {
        var ms = new MemoryStream();
        using (var w = new PptxPackageWriter(ms, 1, null, leaveOpen: true))
        {
            w.AddSlide(new PptxSlide { Image = Png, Format = SlideImageFormat.Jpeg, PixelWidth = 800, PixelHeight = 600 });
            w.Complete();
        }
        ms.Position = 0;
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

        Assert.NotNull(zip.GetEntry("ppt/media/image1.jpeg"));
        Assert.Contains("image1.jpeg", Xml(zip, "ppt/slides/_rels/slide1.xml.rels").ToString());
    }

    [Fact]
    public void Background_UsesTheGivenColour_AndRejectsRubbish()
    {
        using (var zip = Build(1, new PptxOptions { BackgroundHex = "#1e1e1e" }, Slide()))
            Assert.Contains("1E1E1E", Xml(zip, "ppt/slideMasters/slideMaster1.xml").ToString());

        using (var zip = Build(1, new PptxOptions { BackgroundHex = "no-soy-un-color" }, Slide()))
            Assert.Contains("000000", Xml(zip, "ppt/slideMasters/slideMaster1.xml").ToString());
    }

    [Fact]
    public void Complete_WithMissingSlides_Fails()
    {
        var ms = new MemoryStream();
        using var w = new PptxPackageWriter(ms, 3, null, leaveOpen: true);
        w.AddSlide(Slide());
        Assert.Throws<InvalidOperationException>(() => w.Complete());
    }

    [Fact]
    public void AddSlide_BeyondAnnouncedCount_Fails()
    {
        var ms = new MemoryStream();
        using var w = new PptxPackageWriter(ms, 1, null, leaveOpen: true);
        w.AddSlide(Slide());
        Assert.Throws<InvalidOperationException>(() => w.AddSlide(Slide()));
    }

    [Fact]
    public void EmptyPresentation_IsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PptxPackageWriter(new MemoryStream(), 0));
    }

    [Fact]
    public void WriteFile_ProducesAFileThatOpensAsAZip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fmc-test-{Guid.NewGuid():N}.pptx");
        try
        {
            PptxPackageWriter.WriteFile(path, new[] { Slide(), Slide() }, new PptxOptions { Title = "Evento" });

            using var zip = ZipFile.OpenRead(path);
            Assert.Equal(2, zip.Entries.Count(e => e.FullName.StartsWith("ppt/slides/slide")));
            Assert.Contains("Evento", Xml(zip, "docProps/core.xml").ToString());
        }
        finally { File.Delete(path); }
    }
}
