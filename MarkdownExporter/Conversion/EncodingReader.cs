using System.Text;
using MarkdownExporter.Localization;
using UtfUnknown;

namespace MarkdownExporter.Conversion;

public static class EncodingReader
{
    static EncodingReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static string Read(string path, InputEncoding choice)
    {
        var bytes = File.ReadAllBytes(path);
        Encoding encoding;
        if (choice == InputEncoding.Auto)
        {
            if (bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }))
                return new UTF8Encoding(false, true).GetString(bytes, 3, bytes.Length - 3);
            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException) { /* Japanese legacy encoding below. */ }
            var detected = CharsetDetector.DetectFromBytes(bytes).Detected;
            var name = detected?.EncodingName?.ToLowerInvariant();
            if (detected is null || detected.Confidence < .80 || name is null)
                throw new InvalidDataException(LanguageManager.Text("EncodingUnknown"));
            encoding = name switch
            {
                "shift_jis" or "shift-jis" or "windows-31j" or "cp932" => Strict("shift_jis"),
                "euc-jp" or "euc_jp" => Strict("euc-jp"),
                _ => throw new InvalidDataException(LanguageManager.Format("EncodingUnsupported", name))
            };
        }
        else
            encoding = choice switch
            {
                InputEncoding.Utf8 => new UTF8Encoding(false, true),
                InputEncoding.ShiftJis => Strict("shift_jis"),
                InputEncoding.EucJp => Strict("euc-jp"),
                _ => throw new ArgumentOutOfRangeException(nameof(choice))
            };
        var offset = choice == InputEncoding.Utf8 && bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }) ? 3 : 0;
        try
        {
            return encoding.GetString(bytes, offset, bytes.Length - offset);
        }
        catch (DecoderFallbackException ex) { throw new InvalidDataException(LanguageManager.Format("EncodingReadFailed", encoding.WebName), ex); }
    }

    private static Encoding Strict(string name) => Encoding.GetEncoding(name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
}
