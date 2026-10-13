using MarkdownExporter.Conversion;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class AssetWriterTests
{
    [Test]
    public void Store_NameCollision_KeepsExistingFileAndUsesContentSuffix()
    {
        using var folder = new TestFolder();
        var first = folder.Write("source-a/diagram.png", [1, 2, 3]);
        var second = folder.Write("source-b/diagram.png", [4, 5, 6]);
        var writer = new AssetWriter(folder.Root, "report");

        var firstUrl = writer.Store(first, "content");
        var secondUrl = writer.Store(second, "content");

        Assert.Multiple(() =>
        {
            Assert.That(firstUrl, Is.EqualTo("report_assets/content/diagram.png"));
            Assert.That(secondUrl, Does.Match(@"^report_assets/content/diagram-[0-9a-f]{12}\.png$"));
            Assert.That(File.ReadAllBytes(folder.PathFor(firstUrl)), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(File.ReadAllBytes(folder.PathFor(secondUrl)), Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(writer.Store(second, "content"), Is.EqualTo(secondUrl));
        });
    }

    [TestCase("../escape.png")]
    [TestCase("nested/../../escape.png")]
    [TestCase("/absolute.png")]
    public void Store_InvalidRelativeName_IsRejected(string name)
    {
        using var folder = new TestFolder();
        var source = folder.Write("source.png", [1]);
        var writer = new AssetWriter(folder.Root, "report");

        Assert.Throws<InvalidDataException>(() => writer.Store(source, "template", name));
    }

    [Test]
    public void CleanCreated_RemovesOnlyFilesCreatedByWriter()
    {
        using var folder = new TestFolder();
        var existing = folder.Write("report_assets/content/existing.png", [1]);
        var source = folder.Write("source.png", [2]);
        var writer = new AssetWriter(folder.Root, "report");
        var newUrl = writer.Store(source, "template", "icons/new.png");

        writer.CleanCreated();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(existing), Is.True);
            Assert.That(File.Exists(folder.PathFor(newUrl)), Is.False);
            Assert.That(Directory.Exists(folder.PathFor("report_assets/template")), Is.False);
        });
    }
}
