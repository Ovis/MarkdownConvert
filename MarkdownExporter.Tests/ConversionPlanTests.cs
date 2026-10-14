using MarkdownExporter.Conversion;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class ConversionPlanTests
{
    [Test]
    public void Create_WithoutFollow_OnlyPlansExplicitInputs()
    {
        using var folder = new TestFolder();
        var root = folder.Write("source/root.md", "[Next](next.md)");
        folder.Write("source/next.md", "# Next");

        var plan = ConversionPlan.Create([new InputFile(root, InputEncoding.Utf8)], new ConversionOptions());

        Assert.That(plan.Documents.Select(x => x.Path), Is.EqualTo(new[] { root }));
    }

    [Test]
    public void Create_WithFollow_TraversesLinksOnceAndStopsCycles()
    {
        using var folder = new TestFolder();
        var root = folder.Write("source/root.md", "[A](sub/a.md) [Missing](missing.md) [A again](sub/a.md#part)");
        var a = folder.Write("source/sub/a.md", "[Root](../root.md) [B](../b.markdown)");
        var b = folder.Write("source/b.markdown", "# B");
        var options = new ConversionOptions { FollowMarkdownLinks = true, OutputDirectory = folder.PathFor("out") };

        var plan = ConversionPlan.Create([new InputFile(root, InputEncoding.Utf8)], options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Documents.Select(x => x.Path), Is.EqualTo(new[] { root, a, b }));
            Assert.That(plan.Documents[0].Input.Encoding, Is.EqualTo(InputEncoding.Utf8));
            Assert.That(plan.Documents.Skip(1).Select(x => x.Input.Encoding), Is.All.EqualTo(InputEncoding.Auto));
            Assert.That(plan.MissingLinks[root], Is.EqualTo(new[] { "missing.md" }));
        });
    }

    [Test]
    public void Create_CollisionSuffixNeverUsesParentNamesOrStealsNaturalStem()
    {
        using var folder = new TestFolder();
        var first = folder.Write("private-user/a/intro.md", "# First");
        var second = folder.Write("other/b/intro.md", "# Second");
        var natural = folder.Write("other/intro-2.md", "# Natural");
        var output = folder.PathFor("out");
        var options = new ConversionOptions { OutputDirectory = output };

        var plan = ConversionPlan.Create([
            new InputFile(first, InputEncoding.Auto),
            new InputFile(second, InputEncoding.Auto),
            new InputFile(natural, InputEncoding.Auto)
        ], options);

        Assert.That(plan.Documents.Select(x => x.Stem), Is.EqualTo(new[] { "intro", "intro-3", "intro-2" }));
        Assert.That(plan.Documents.Select(x => x.HtmlPath), Is.All.StartWith(output));
    }

    [Test]
    public void Create_FollowedDocumentUsesItsOwnDirectoryWhenOutputIsUnspecified()
    {
        using var folder = new TestFolder();
        var root = folder.Write("source/root.md", "[Outside](../outside/target.md)");
        var target = folder.Write("outside/target.md", "# Target");
        var options = new ConversionOptions { FollowMarkdownLinks = true };

        var plan = ConversionPlan.Create([new InputFile(root, InputEncoding.Auto)], options);

        Assert.That(plan.Documents.Single(x => x.Path == target).HtmlPath, Is.EqualTo(folder.PathFor("outside/target.html")));
    }

    [Test]
    public void Find_IgnoresImagesAndRemoteLinksButIncludesSafeHtmlAnchors()
    {
        using var folder = new TestFolder();
        var source = folder.Write("source/index.md", """
            [Chapter](../chapter.md#part)
            <a href="next.markdown?view=all">Next</a>
            ![Not a document](picture.md)
            [Remote](https://example.com/remote.md)
            """);

        var links = LinkedMarkdownLinks.Find(source, File.ReadAllText(source));

        Assert.That(links.Select(x => x.TargetPath), Is.EqualTo(new[]
        {
            folder.PathFor("chapter.md"), folder.PathFor("source/next.markdown")
        }));
    }
}
