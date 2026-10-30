using AngleSharp.Html.Parser;
using MarkdownExporter.Conversion;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class MarkdownDocumentBuilderTests
{
    [Test]
    public void Build_LocalImage_CopiesToDocumentAssets()
    {
        using var folder = new TestFolder();
        var input = folder.Write("source/report.md", "![diagram](image.png)");
        folder.Write("source/image.png", [1, 2, 3]);
        var output = folder.PathFor("output");
        var html = Build(input, File.ReadAllText(input), output, new ConversionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("report_assets/content/image.png"));
            Assert.That(File.ReadAllBytes(folder.PathFor("output/report_assets/content/image.png")),
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void Build_EmbedImages_WritesDataUriWithoutAssets()
    {
        using var folder = new TestFolder();
        var input = folder.Write("source/report.md", "![diagram](image.png)");
        folder.Write("source/image.png", [1, 2, 3]);
        var output = folder.PathFor("output");

        var html = Build(input, File.ReadAllText(input), output,
            new ConversionOptions { EmbedImages = true });

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("data:image/png;base64,AQID"));
            Assert.That(Directory.Exists(folder.PathFor("output/report_assets")), Is.False);
        });
    }

    [Test]
    public void Build_MissingLocalImage_FailsBeforeCreatingAssets()
    {
        using var folder = new TestFolder();
        var input = folder.Write("source/report.md", "![missing](missing.png)");
        var output = folder.PathFor("output");

        Assert.Throws<FileNotFoundException>(() =>
            Build(input, File.ReadAllText(input), output, new ConversionOptions()));
        Assert.That(Directory.Exists(folder.PathFor("output/report_assets")), Is.False);
    }

    [Test]
    public void Build_RawHtml_RemovesUnsafeMarkupAndKeepsFormatting()
    {
        using var folder = new TestFolder();
        var input = folder.Write("report.md", "<script>alert(1)</script><strong>safe</strong><a href='javascript:alert(1)' onclick='bad()'>link</a>");

        var html = Build(input, File.ReadAllText(input), folder.PathFor("output"), new ConversionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Not.Contain("<script"));
            Assert.That(html, Does.Not.Contain("onclick"));
            Assert.That(html, Does.Not.Contain("javascript:"));
            Assert.That(html, Does.Contain("<strong>safe</strong>"));
        });
    }

    [Test]
    public void Build_IncludedMarkdownLink_RecordsFallbackForFinalization()
    {
        using var folder = new TestFolder();
        var input = folder.Write("source/report.md", "[Next](docs/next.md#section)");
        var output = folder.PathFor("output");
        var target = folder.PathFor("source/docs/next.md");
        var targets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [target] = folder.PathFor("output/next.html")
        };

        var html = Build(input, File.ReadAllText(input), output, new ConversionOptions(), targets);

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("href=\"next.html#section\""));
            Assert.That(html, Does.Contain("data-mdexport-original=\"docs/next.md#section\""));
        });
    }

    [Test]
    public void Build_PageBreak_AddsMarkerAndPrintRule()
    {
        using var folder = new TestFolder();
        var input = folder.Write("report.md", "first\n\n<!-- pagebreak -->\n\nsecond");

        var html = Build(input, File.ReadAllText(input), folder.PathFor("output"), new ConversionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("class=\"pagebreak\""));
            Assert.That(html, Does.Contain("break-before:page"));
        });
    }

    [TestCase("\n", false)]
    [TestCase("\n", true)]
    [TestCase("\r\n", false)]
    [TestCase("\r\n", true)]
    public void Build_PageBreak_PreservesMarkdownAfterMarker(string newline, bool includeBlankLines)
    {
        using var folder = new TestFolder();
        var spacer = includeBlankLines ? newline : "";
        var input = folder.Write("report.md",
            $"first{newline}{spacer}<!-- pagebreak -->{newline}{spacer}## 2. Team plan{newline}{newline}This is **bold**.");

        var html = Build(input, File.ReadAllText(input), folder.PathFor("output"), new ConversionOptions());
        var document = new HtmlParser().ParseDocument(html);

        Assert.Multiple(() =>
        {
            Assert.That(document.QuerySelector(".pagebreak"), Is.Not.Null);
            Assert.That(document.QuerySelector("h2")?.TextContent, Is.EqualTo("2. Team plan"));
            Assert.That(document.QuerySelector("strong")?.TextContent, Is.EqualTo("bold"));
        });
    }

    [Test]
    public void Build_Math_CopiesKaTeXLicenseWithFonts()
    {
        using var folder = new TestFolder();
        var input = folder.Write("report.md", "<span class=\"math\">x</span>");
        var output = folder.PathFor("output");

        var html = Build(input, File.ReadAllText(input), output, new ConversionOptions());
        var license = folder.PathFor("output/report_assets/template/fonts/LICENSE-KaTeX.txt");

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("report_assets/template/fonts/KaTeX_Main-Regular.woff2"));
            Assert.That(File.Exists(license), Is.True);
            Assert.That(File.ReadAllText(license), Does.Contain("SIL OPEN FONT LICENSE Version 1.1"));
        });
    }

    private static string Build(string input, string markdown, string output, ConversionOptions options,
        IReadOnlyDictionary<string, string>? targets = null)
    {
        var template = new TemplateDocument(Path.GetDirectoryName(input)!,
            "<!doctype html><html><head><style>{{style}}</style></head><body>{{content}}</body></html>",
            "body { color: black; }");
        var assets = new AssetWriter(output, Path.GetFileNameWithoutExtension(input));
        return new MarkdownDocumentBuilder().Build(input, markdown, output,
            Path.GetFileNameWithoutExtension(input), options, template, assets, targets ?? new Dictionary<string, string>());
    }
}
