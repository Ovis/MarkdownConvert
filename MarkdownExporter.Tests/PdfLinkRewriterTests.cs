using MarkdownExporter.Conversion;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Annotations;
using PdfSharp.Pdf.IO;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class PdfLinkRewriterTests
{
    [Test]
    public void RewriteLocalPdfLinks_UsesRelativeGoToRAndKeepsExternalUrl()
    {
        using var folder = new TestFolder();
        var input = folder.PathFor("input.pdf");
        var output = folder.PathFor("portable.pdf");
        var target = folder.PathFor("linked target.pdf");
        var localUrl = new Uri(target).AbsoluteUri;
        CreatePdf(input, localUrl, localUrl + "#page=2", "https://example.com/");

        var changed = PdfLinkRewriter.RewriteLocalPdfLinks(input, output, output);

        using var document = PdfReader.Open(output, PdfDocumentOpenMode.Modify);
        var first = Action(document, 0);
        var second = Action(document, 1);
        var external = Action(document, 2);
        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(document.PageCount, Is.EqualTo(1));
            Assert.That(first.Elements.GetName("/S"), Is.EqualTo("/GoToR"));
            Assert.That(first.Elements.GetString("/F"), Is.EqualTo("linked target.pdf"));
            Assert.That(first.Elements.ContainsKey("/URI"), Is.False);
            Assert.That(second.Elements.GetName("/S"), Is.EqualTo("/GoToR"));
            Assert.That(second.Elements.GetString("/F"), Is.EqualTo("linked target.pdf"));
            Assert.That(second.Elements.GetArray("/D")!.Elements.GetInteger(0), Is.EqualTo(1));
            Assert.That(external.Elements.GetString("/URI"), Is.EqualTo("https://example.com/"));
        });
    }

    [Test]
    public void RewriteLocalPdfLinks_UnknownFragment_KeepsRelativeUri()
    {
        using var folder = new TestFolder();
        var input = folder.PathFor("input.pdf");
        var output = folder.PathFor("portable.pdf");
        var target = folder.PathFor("日本語 target.pdf");
        CreatePdf(input, new Uri(target).AbsoluteUri + "#chapter");

        PdfLinkRewriter.RewriteLocalPdfLinks(input, output, output);

        using var document = PdfReader.Open(output, PdfDocumentOpenMode.Modify);
        var action = Action(document, 0);
        Assert.Multiple(() =>
        {
            Assert.That(action.Elements.GetName("/S"), Is.EqualTo("/URI"));
            Assert.That(action.Elements.GetString("/URI"),
                Is.EqualTo("%E6%97%A5%E6%9C%AC%E8%AA%9E%20target.pdf#chapter"));
        });
    }

    [Test]
    public void RewriteLocalPdfLinks_NoLocalPdfLink_DoesNotWriteSecondFile()
    {
        using var folder = new TestFolder();
        var input = folder.PathFor("input.pdf");
        var output = folder.PathFor("portable.pdf");
        CreatePdf(input, "https://example.com/", new Uri(folder.PathFor("readme.txt")).AbsoluteUri);

        var changed = PdfLinkRewriter.RewriteLocalPdfLinks(input, output, output);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.False);
            Assert.That(File.Exists(output), Is.False);
            Assert.That(File.Exists(input), Is.True);
        });
    }

    [Test]
    public void RewriteLocalPdfLinks_DifferentVolume_RejectsAbsoluteFallback()
    {
        using var folder = new TestFolder();
        var input = folder.PathFor("input.pdf");
        var output = folder.PathFor("portable.pdf");
        var otherDrive = Path.GetPathRoot(folder.Root)!.StartsWith("C:", StringComparison.OrdinalIgnoreCase)
            ? "Z:\\linked.pdf" : "C:\\linked.pdf";
        CreatePdf(input, new Uri(otherDrive).AbsoluteUri);

        Assert.Throws<InvalidOperationException>(() =>
            PdfLinkRewriter.RewriteLocalPdfLinks(input, output, output));
        Assert.That(File.Exists(output), Is.False);
    }

    private static PdfDictionary Action(PdfDocument document, int index) =>
        document.Pages[0].Annotations[index].Elements.GetDictionary("/A")!;

    private static void CreatePdf(string path, params string[] urls)
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        foreach (var url in urls)
            page.Annotations.Add(PdfLinkAnnotation.CreateWebLink(new PdfRectangle(new XRect(0, 0, 100, 20)), url));
        document.Save(path);
    }
}
