using MarkdownExporter.Conversion;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class CommandLineParserTests
{
    [Test]
    public void Parse_RunOptions_AppliesEachOption()
    {
        var command = CommandLineParser.Parse([
            "--run", "first.md", "second.markdown", "-o", "output", "--template", "custom",
            "--format", "pdf", "--encoding", "euc-jp", "--orientation", "landscape",
            "--embed-images", "--fit-figures-to-page", "--overwrite"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(command.Run, Is.True);
            Assert.That(command.Files, Is.EqualTo(new[] { "first.md", "second.markdown" }));
            Assert.That(command.Options.OutputDirectory, Is.EqualTo("output"));
            Assert.That(command.Options.Template, Is.EqualTo("custom"));
            Assert.That(command.Options.Format, Is.EqualTo(ExportFormat.Pdf));
            Assert.That(command.Options.Encoding, Is.EqualTo(InputEncoding.EucJp));
            Assert.That(command.Options.Orientation, Is.EqualTo(PageOrientation.Landscape));
            Assert.That(command.Options.EmbedImages, Is.True);
            Assert.That(command.Options.FitFiguresToPage, Is.True);
            Assert.That(command.Options.Overwrite, Is.True);
        });
    }

    [Test]
    public void Parse_DroppedFiles_OpensGuiWithCliDefaults()
    {
        var command = CommandLineParser.Parse(["first.md", "two spaces.md"]);

        Assert.Multiple(() =>
        {
            Assert.That(command.Run, Is.False);
            Assert.That(command.Files, Is.EqualTo(new[] { "first.md", "two spaces.md" }));
            Assert.That(command.Options.Format, Is.EqualTo(ExportFormat.Both));
            Assert.That(command.Options.Encoding, Is.EqualTo(InputEncoding.Auto));
        });
    }

    [TestCase("--run")]
    [TestCase("--run a.md --format other")]
    [TestCase("--run a.md --encoding")]
    [TestCase("--run a.md --unknown")]
    [TestCase("--format pdf a.md")]
    public void Parse_InvalidArguments_Throws(string arguments)
    {
        Assert.Throws<ArgumentException>(() => CommandLineParser.Parse(arguments.Split(' ')));
    }
}
