using System.Text;
using MarkdownExporter.Conversion;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class EncodingReaderTests
{
    [OneTimeSetUp]
    public void RegisterCodePages() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [TestCase(InputEncoding.Auto)]
    [TestCase(InputEncoding.Utf8)]
    public void Read_Utf8Bom_DoesNotReturnBom(InputEncoding choice)
    {
        using var folder = new TestFolder();
        var path = folder.Write("utf8.md", [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes("日本語")]);

        Assert.That(EncodingReader.Read(path, choice), Is.EqualTo("日本語"));
    }

    [TestCase(InputEncoding.ShiftJis, "shift_jis")]
    [TestCase(InputEncoding.EucJp, "euc-jp")]
    public void Read_ExplicitJapaneseEncoding_DecodesText(InputEncoding choice, string encodingName)
    {
        using var folder = new TestFolder();
        var expected = "日本語の文章と記号。";
        var path = folder.Write("legacy.md", Encoding.GetEncoding(encodingName).GetBytes(expected));

        Assert.That(EncodingReader.Read(path, choice), Is.EqualTo(expected));
    }

    [TestCase("shift_jis")]
    [TestCase("euc-jp")]
    public void Read_AutoDetectsJapaneseLegacyEncoding(string encodingName)
    {
        using var folder = new TestFolder();
        const string sentence = "これは文章の文字コードを確認するための日本語サンプルです。漢字、ひらがな、カタカナを含みます。\n";
        var expected = string.Concat(Enumerable.Repeat(sentence, 12));
        var path = folder.Write("legacy.md", Encoding.GetEncoding(encodingName).GetBytes(expected));

        Assert.That(EncodingReader.Read(path, InputEncoding.Auto), Is.EqualTo(expected));
    }

    [Test]
    public void Read_InvalidUtf8_ReportsDecodeFailure()
    {
        using var folder = new TestFolder();
        var path = folder.Write("invalid.md", [0xc3, 0x28]);

        Assert.Throws<InvalidDataException>(() => EncodingReader.Read(path, InputEncoding.Utf8));
    }
}
