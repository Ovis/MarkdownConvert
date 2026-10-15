using MarkdownExporter.Conversion;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class RelativeLinkAnalyzerTests
{
    [Test]
    public void Find_ReportsOnlyLocalLinksWhoseBaseChanges()
    {
        using var folder = new TestFolder();
        var input = folder.Write("source/report.md", """
            [PDF](docs/manual.pdf#page=2) [Markdown](other.md)
            [Converted](next.md) [Web](https://example.com/) [Here](#heading)
            ![Image](picture.png)
            <a href="files/data.csv">CSV</a>
            """);
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            folder.PathFor("source/next.md")
        };

        var links = RelativeLinkAnalyzer.Find(input, File.ReadAllText(input), folder.PathFor("output"), targets);

        Assert.That(links, Is.EquivalentTo(new[] { "docs/manual.pdf#page=2", "other.md", "files/data.csv" }));
        Assert.That(RelativeLinkAnalyzer.Find(input, File.ReadAllText(input),
            folder.PathFor("source"), targets), Is.Empty);
    }

    [Test]
    public void Scan_SkipsMarkdownConvertedInEitherFormat()
    {
        using var folder = new TestFolder();
        var input = folder.Write("source/report.md", "[Next](next.md)");
        var next = folder.Write("source/next.md", "# Next");
        var files = new[] { new InputFile(input, InputEncoding.Utf8), new InputFile(next, InputEncoding.Utf8) };
        var options = new ConversionOptions { OutputDirectory = folder.PathFor("output"), Format = ExportFormat.Html };

        Assert.That(RelativeLinkAnalyzer.Scan(files, options), Is.Empty);

        options.Format = ExportFormat.Pdf;
        Assert.That(RelativeLinkAnalyzer.Scan(files, options), Is.Empty);
    }
}
