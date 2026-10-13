using MarkdownExporter.Conversion;

namespace MarkdownExporter.Settings;

internal sealed class ConversionDefaultsChanges
{
    public ConversionDefaultsChanges(ConversionOptions previous, ConversionOptions saved)
    {
        Saved = saved.Copy();
        FormatChanged = previous.Format != saved.Format;
        OutputDirectoryChanged = !string.Equals(NormalizeOutputDirectory(previous.OutputDirectory),
            NormalizeOutputDirectory(saved.OutputDirectory), StringComparison.Ordinal);
        TemplateChanged = !string.Equals(previous.Template, saved.Template, StringComparison.Ordinal);
        EmbedImagesChanged = previous.EmbedImages != saved.EmbedImages;
        OrientationChanged = previous.Orientation != saved.Orientation;
        FitFiguresToPageChanged = previous.FitFiguresToPage != saved.FitFiguresToPage;
    }

    public ConversionOptions Saved { get; }
    public bool FormatChanged { get; }
    public bool OutputDirectoryChanged { get; }
    public bool TemplateChanged { get; }
    public bool EmbedImagesChanged { get; }
    public bool OrientationChanged { get; }
    public bool FitFiguresToPageChanged { get; }
    public bool HasChanges => FormatChanged || OutputDirectoryChanged || TemplateChanged || EmbedImagesChanged
        || OrientationChanged || FitFiguresToPageChanged;

    private static string? NormalizeOutputDirectory(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
