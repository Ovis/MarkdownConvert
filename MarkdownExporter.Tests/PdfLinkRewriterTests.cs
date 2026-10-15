using System.Diagnostics;
using System.Text.Json.Nodes;
using MarkdownExporter.Conversion;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class PdfLinkRewriterTests
{
    private static string QpdfPath => Environment.GetEnvironmentVariable("MDEXPORT_TEST_QPDF")
        ?? Path.Combine(TestContext.CurrentContext.TestDirectory, "Tools", "qpdf", "qpdf.exe");

    [Test]
    public void QpdfProcess_DoesNotOpenAConsoleWindow()
    {
        var start = PdfLinkRewriter.CreateStartInfo(QpdfPath, "--version");
        Assert.Multiple(() =>
        {
            Assert.That(start.UseShellExecute, Is.False);
            Assert.That(start.CreateNoWindow, Is.True);
            Assert.That(start.WindowStyle, Is.EqualTo(ProcessWindowStyle.Hidden));
            Assert.That(start.ArgumentList, Is.EquivalentTo(new[] { "--version" }));
        });
    }

    [Test]
    public async Task RewriteLocalPdfLinks_UsesRelativeGoToRAndKeepsExternalUrl()
    {
        using var folder = new TestFolder();
        var input = folder.PathFor("input.pdf");
        var output = folder.PathFor("portable.pdf");
        var target = folder.PathFor("linked target.pdf");
        var localUrl = new Uri(target).AbsoluteUri;
        await CreatePdfAsync(input, localUrl, localUrl + "#page=2", "https://example.com/");

        var changed = await PdfLinkRewriter.RewriteLocalPdfLinksAsync(input, output, output, QpdfPath);

        var actions = await ReadActionsAsync(output);
        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(actions[0]["/S"]?.GetValue<string>(), Is.EqualTo("/GoToR"));
            Assert.That(actions[0]["/F"]?.GetValue<string>(), Is.EqualTo("u:linked target.pdf"));
            Assert.That(actions[0].ContainsKey("/URI"), Is.False);
            Assert.That(actions[1]["/S"]?.GetValue<string>(), Is.EqualTo("/GoToR"));
            Assert.That(actions[1]["/D"]?[0]?.GetValue<int>(), Is.EqualTo(1));
            Assert.That(actions[2]["/URI"]?.GetValue<string>(), Is.EqualTo("u:https://example.com/"));
        });
    }

    [Test]
    public async Task RewriteLocalPdfLinks_UnknownFragment_KeepsRelativeUri()
    {
        using var folder = new TestFolder();
        var input = folder.PathFor("input.pdf");
        var output = folder.PathFor("portable.pdf");
        var target = folder.PathFor("日本語 target.pdf");
        await CreatePdfAsync(input, new Uri(target).AbsoluteUri + "#chapter");

        await PdfLinkRewriter.RewriteLocalPdfLinksAsync(input, output, output, QpdfPath);

        var actions = await ReadActionsAsync(output);
        Assert.Multiple(() =>
        {
            Assert.That(actions[0]["/S"]?.GetValue<string>(), Is.EqualTo("/URI"));
            Assert.That(actions[0]["/URI"]?.GetValue<string>(),
                Is.EqualTo("u:%E6%97%A5%E6%9C%AC%E8%AA%9E%20target.pdf#chapter"));
        });
    }

    [Test]
    public async Task RewriteLocalPdfLinks_IndirectAction_IsUpdated()
    {
        using var folder = new TestFolder();
        var input = folder.PathFor("input.pdf");
        var output = folder.PathFor("portable.pdf");
        await CreatePdfAsync(input, [new Uri(folder.PathFor("linked.pdf")).AbsoluteUri], true);

        var changed = await PdfLinkRewriter.RewriteLocalPdfLinksAsync(input, output, output, QpdfPath);

        var actions = await ReadActionsAsync(output);
        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(actions[0]["/S"]?.GetValue<string>(), Is.EqualTo("/GoToR"));
            Assert.That(actions[0]["/F"]?.GetValue<string>(), Is.EqualTo("u:linked.pdf"));
        });
    }

    [Test]
    public async Task RewriteLocalPdfLinks_NoLocalPdfLink_DoesNotWriteSecondFile()
    {
        using var folder = new TestFolder();
        var input = folder.PathFor("input.pdf");
        var output = folder.PathFor("portable.pdf");
        await CreatePdfAsync(input, "https://example.com/", new Uri(folder.PathFor("readme.txt")).AbsoluteUri);

        var changed = await PdfLinkRewriter.RewriteLocalPdfLinksAsync(input, output, output, QpdfPath);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.False);
            Assert.That(File.Exists(output), Is.False);
            Assert.That(File.Exists(input), Is.True);
        });
    }

    [Test]
    public async Task RewriteLocalPdfLinks_DifferentVolume_RejectsAbsoluteFallback()
    {
        using var folder = new TestFolder();
        var input = folder.PathFor("input.pdf");
        var output = folder.PathFor("portable.pdf");
        var otherDrive = Path.GetPathRoot(folder.Root)!.StartsWith("C:", StringComparison.OrdinalIgnoreCase)
            ? "Z:\\linked.pdf" : "C:\\linked.pdf";
        await CreatePdfAsync(input, new Uri(otherDrive).AbsoluteUri);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await PdfLinkRewriter.RewriteLocalPdfLinksAsync(input, output, output, QpdfPath));
        Assert.That(File.Exists(output), Is.False);
    }

    [Test]
    public async Task RewriteLocalPdfLinks_UnconvertedMarkdown_RestoresOriginalRelativeLink()
    {
        using var folder = new TestFolder();
        var input = folder.PathFor("input.pdf");
        var output = folder.PathFor("portable.pdf");
        var target = folder.PathFor("docs/next.md");
        var absolute = new Uri(target).AbsoluteUri + "#section";
        await CreatePdfAsync(input, absolute);

        var changed = await PdfLinkRewriter.RewriteLocalPdfLinksAsync(input, output, output, QpdfPath,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [absolute] = "docs/next.md#section"
            });

        var actions = await ReadActionsAsync(output);
        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(actions[0]["/URI"]?.GetValue<string>(), Is.EqualTo("u:docs/next.md#section"));
        });
    }

    private static Task CreatePdfAsync(string path, params string[] urls) => CreatePdfAsync(path, urls, false);

    private static async Task CreatePdfAsync(string path, string[] urls, bool indirectAction)
    {
        var objects = new JsonObject
        {
            ["obj:1 0 R"] = new JsonObject { ["value"] = new JsonObject { ["/Type"] = "/Catalog", ["/Pages"] = "2 0 R" } },
            ["obj:2 0 R"] = new JsonObject { ["value"] = new JsonObject { ["/Type"] = "/Pages", ["/Kids"] = new JsonArray("3 0 R"), ["/Count"] = 1 } },
            ["obj:3 0 R"] = new JsonObject
            {
                ["value"] = new JsonObject
                {
                    ["/Type"] = "/Page",
                    ["/Parent"] = "2 0 R",
                    ["/MediaBox"] = new JsonArray(0, 0, 612, 792),
                    ["/Resources"] = new JsonObject(),
                    ["/Annots"] = new JsonArray(urls.Select((_, index) => (JsonNode?)JsonValue.Create($"{index + 4} 0 R")).ToArray())
                }
            },
            ["trailer"] = new JsonObject
            {
                ["value"] = new JsonObject
                { ["/Root"] = "1 0 R", ["/Size"] = urls.Length + (indirectAction ? 5 : 4) }
            }
        };
        for (var index = 0; index < urls.Length; index++)
        {
            objects[$"obj:{index + 4} 0 R"] = new JsonObject
            {
                ["value"] = new JsonObject
                {
                    ["/Type"] = "/Annot",
                    ["/Subtype"] = "/Link",
                    ["/Rect"] = new JsonArray(0, 0, 100, 20),
                    ["/A"] = indirectAction ? JsonValue.Create($"{urls.Length + 4} 0 R") :
                    new JsonObject { ["/S"] = "/URI", ["/URI"] = "u:" + urls[index] }
                }
            };
        }
        if (indirectAction)
            objects[$"obj:{urls.Length + 4} 0 R"] = new JsonObject
            {
                ["value"] = new JsonObject
                { ["/S"] = "/URI", ["/URI"] = "u:" + urls[0] }
            };
        var source = Path.ChangeExtension(path, ".json");
        var json = new JsonObject
        {
            ["qpdf"] = new JsonArray(new JsonObject
            { ["jsonversion"] = 2, ["pdfversion"] = "1.4" }, objects)
        };
        await File.WriteAllTextAsync(source, json.ToJsonString());
        await RunQpdfAsync("--json-input", source, path);
    }

    private static async Task<JsonObject[]> ReadActionsAsync(string path)
    {
        var jsonPath = Path.ChangeExtension(path, ".json");
        await RunQpdfAsync("--json-output", path, jsonPath, "--json-stream-data=none");
        var objects = JsonNode.Parse(await File.ReadAllTextAsync(jsonPath))!["qpdf"]![1]!.AsObject();
        return objects.Where(entry => entry.Key.StartsWith("obj:", StringComparison.Ordinal))
            .Select(entry => entry.Value?["value"] as JsonObject)
            .Where(value => value?["/Subtype"]?.GetValue<string>() == "/Link")
            .Select(value => value!["/A"] is JsonObject direct ? direct :
                objects["obj:" + value["/A"]!.GetValue<string>()]!["value"]!.AsObject()).ToArray();
    }

    private static async Task RunQpdfAsync(params string[] arguments)
    {
        var start = new ProcessStartInfo(QpdfPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.That(process.ExitCode, Is.EqualTo(0), await error);
    }
}
