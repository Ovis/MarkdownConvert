using MarkdownExporter.Conversion;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class ConversionPreflightTests
{
    [Test]
    public void Preflight_MissingAndUnsupportedInput_ReportsBoth()
    {
        using var folder = new TestFolder();
        var missing = folder.PathFor("missing.txt");

        var issues = ConversionService.Preflight([new InputFile(missing, InputEncoding.Auto)], new ConversionOptions());

        Assert.That(issues.Select(x => x.Kind), Is.EquivalentTo(new[]
        {
            PreflightIssueKind.MissingInput, PreflightIssueKind.InvalidMarkdown
        }));
    }

    [Test]
    public void Preflight_SameStemInOneOutputDirectory_ReportsCollision()
    {
        using var folder = new TestFolder();
        var first = folder.Write("a/report.md", "first");
        var second = folder.Write("b/report.markdown", "second");
        var options = new ConversionOptions { Format = ExportFormat.Both, OutputDirectory = folder.PathFor("out") };

        var issues = ConversionService.Preflight([
            new InputFile(first, InputEncoding.Auto), new InputFile(second, InputEncoding.Auto)
        ], options);

        Assert.That(issues.Count(x => x.Kind == PreflightIssueKind.DuplicateOutput), Is.EqualTo(2));
    }

    [Test]
    public void Preflight_ExistingOutput_RequiresOverwrite()
    {
        using var folder = new TestFolder();
        var input = folder.Write("report.md", "source");
        folder.Write("report.pdf", "existing output");
        var options = new ConversionOptions { Format = ExportFormat.Pdf };
        var files = new[] { new InputFile(input, InputEncoding.Auto) };

        Assert.That(ConversionService.Preflight(files, options).Select(x => x.Kind),
            Is.EqualTo(new[] { PreflightIssueKind.OutputExists }));

        options.Overwrite = true;
        Assert.That(ConversionService.Preflight(files, options), Is.Empty);
    }
}
