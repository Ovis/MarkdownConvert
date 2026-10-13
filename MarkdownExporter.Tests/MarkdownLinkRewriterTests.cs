using MarkdownExporter.Conversion;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class MarkdownLinkRewriterTests
{
    [Test]
    public void Rewrite_SuccessfulTarget_KeepsHtmlLinkAndRemovesInternalMarker()
    {
        using var folder = new TestFolder();
        var source = folder.PathFor("source.md");
        var target = folder.PathFor("docs/next.md");
        var warnings = new List<string>();
        const string html = "<html><body><a href='next.html#section' data-mdexport-original='docs/next.md#section'>Next</a></body></html>";

        var result = MarkdownLinkRewriter.Rewrite(html, source,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { target }, warnings, true);

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain("href=\"next.html#section\""));
            Assert.That(result, Does.Not.Contain("data-mdexport-original"));
            Assert.That(warnings, Is.Empty);
        });
    }

    [Test]
    public void Rewrite_FailedTarget_RestoresMarkdownLinkAndReportsOnce()
    {
        using var folder = new TestFolder();
        var source = folder.PathFor("source.md");
        var warnings = new List<string>();
        const string html = "<html><body><a href='next.html' data-mdexport-original='docs/next.md'>Next</a></body></html>";

        var result = MarkdownLinkRewriter.Rewrite(html, source,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase), warnings, true);
        MarkdownLinkRewriter.Rewrite(html, source,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase), warnings, false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain("href=\"docs/next.md\""));
            Assert.That(result, Does.Not.Contain("data-mdexport-original"));
            Assert.That(warnings, Has.Count.EqualTo(1));
        });
    }
}
