using MarkdownExporter.Conversion;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class TemplateStoreTests
{
    [Test]
    public void Initialize_CopiesBundledDefaultOnce()
    {
        using var folder = new TestFolder();
        var bundled = folder.PathFor("bundled");
        folder.Write("bundled/index.html", "<html>{{content}}<style>{{style}}</style></html>");
        folder.Write("bundled/style.css", "body { color: blue; }");
        var root = folder.PathFor("appdata/Template");

        TemplateStore.Initialize(root, bundled);
        folder.Write("appdata/Template/default/style.css", "user customization");
        TemplateStore.Initialize(root, bundled);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(folder.PathFor("appdata/Template/default/index.html")), Is.True);
            Assert.That(File.ReadAllText(folder.PathFor("appdata/Template/default/style.css")),
                Is.EqualTo("user customization"));
        });
    }

    [Test]
    public void Initialize_MissingBundledTemplate_Throws()
    {
        using var folder = new TestFolder();

        Assert.Throws<DirectoryNotFoundException>(() =>
            TemplateStore.Initialize(folder.PathFor("appdata/Template"), folder.PathFor("missing")));
    }

    [Test]
    public void Load_AbsoluteCustomTemplate_ReturnsContents()
    {
        using var folder = new TestFolder();
        folder.Write("custom/index.html", "<html><body>{{content}}<style>{{style}}</style></body></html>");
        folder.Write("custom/style.css", "body { color: blue; }");

        var template = TemplateStore.Load(folder.PathFor("custom"));

        Assert.Multiple(() =>
        {
            Assert.That(template.Directory, Is.EqualTo(folder.PathFor("custom")));
            Assert.That(template.Index, Does.Contain("{{content}}"));
            Assert.That(template.Style, Does.Contain("color: blue"));
        });
    }

    [TestCase("<html><script>alert(1)</script>{{content}}<style>{{style}}</style></html>", "body{}")]
    [TestCase("<html><div onclick='run()'>{{content}}</div><style>{{style}}</style></html>", "body{}")]
    [TestCase("<html>{{content}}<style>{{style}}</style></html>", "@import url(other.css);")]
    public void Load_UnsafeTemplate_IsRejected(string index, string style)
    {
        using var folder = new TestFolder();
        folder.Write("custom/index.html", index);
        folder.Write("custom/style.css", style);

        Assert.Throws<InvalidDataException>(() => TemplateStore.Load(folder.PathFor("custom")));
    }

    [Test]
    public void Load_MissingPlaceholder_IsRejected()
    {
        using var folder = new TestFolder();
        folder.Write("custom/index.html", "<html>{{content}}</html>");
        folder.Write("custom/style.css", "body{}");

        Assert.Throws<InvalidDataException>(() => TemplateStore.Load(folder.PathFor("custom")));
    }

    [TestCase("..")]
    [TestCase("nested/template")]
    public void Load_InvalidTemplateName_IsRejected(string name)
    {
        Assert.Throws<ArgumentException>(() => TemplateStore.Load(name));
    }
}
