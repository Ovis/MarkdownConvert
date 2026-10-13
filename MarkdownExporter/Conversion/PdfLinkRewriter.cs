using MarkdownExporter.Localization;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace MarkdownExporter.Conversion;

internal static class PdfLinkRewriter
{
    public static bool RewriteLocalPdfLinks(string inputPath, string outputPath, string finalPath)
    {
        using var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(finalPath))!;
        var changed = false;
        foreach (var page in document.Pages)
        {
            for (var index = 0; index < page.Annotations.Count; index++)
            {
                var annotation = page.Annotations[index];
                if (annotation.Elements.GetName("/Subtype") != "/Link")
                    continue;
                var action = annotation.Elements.GetDictionary("/A");
                if (action?.Elements.GetName("/S") != "/URI")
                    continue;
                var url = action.Elements.GetString("/URI");
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.IsFile ||
                    !Path.GetExtension(uri.LocalPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    continue;

                var relative = Path.GetRelativePath(outputDirectory, uri.LocalPath).Replace('\\', '/');
                if (Path.IsPathFullyQualified(relative))
                    throw new InvalidOperationException(LanguageManager.Format("PdfLinkDifferentVolume", uri.LocalPath));
                if (uri.Query.Length == 0 && TryGetPageIndex(uri.Fragment, out var pageIndex))
                {
                    action.Elements.Remove("/URI");
                    action.Elements.SetName("/S", "/GoToR");
                    action.Elements.SetString("/F", relative);
                    var destination = new PdfArray(document, new PdfInteger(pageIndex), new PdfName("/Fit"));
                    action.Elements.SetObject("/D", destination);
                    action.Elements.SetBoolean("/NewWindow", true);
                }
                else
                {
                    var escaped = string.Join('/', relative.Split('/').Select(segment =>
                        segment is "." or ".." ? segment : Uri.EscapeDataString(segment)));
                    action.Elements.SetString("/URI", escaped + uri.Query + uri.Fragment);
                }
                changed = true;
            }
        }
        if (changed)
            document.Save(outputPath);
        return changed;
    }

    private static bool TryGetPageIndex(string fragment, out int pageIndex)
    {
        pageIndex = 0;
        if (fragment.Length == 0)
            return true;
        if (fragment.StartsWith("#page=", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(fragment.AsSpan(6), out var page) && page > 0)
        {
            pageIndex = page - 1;
            return true;
        }
        return false;
    }
}
