using AngleSharp.Html.Parser;
using MarkdownExporter.Localization;

namespace MarkdownExporter.Conversion;

internal static class MarkdownLinkRewriter
{
    public static string Rewrite(string html, string sourcePath, HashSet<string> successful,
        List<string> warnings, bool reportWarnings)
    {
        if (!html.Contains("data-mdexport-original", StringComparison.Ordinal))
            return html;
        var document = new HtmlParser().ParseDocument(html);
        foreach (var link in document.QuerySelectorAll("a[data-mdexport-original]"))
        {
            var original = link.GetAttribute("data-mdexport-original")!;
            var pathPart = original.Split('?', '#')[0];
            var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!,
                Uri.UnescapeDataString(pathPart).Replace('/', Path.DirectorySeparatorChar)));
            if (!successful.Contains(target))
            {
                link.SetAttribute("href", original);
                if (reportWarnings)
                    warnings.Add(LanguageManager.Format("LinkTargetFailed", original));
            }
            link.RemoveAttribute("data-mdexport-original");
        }
        return "<!doctype html>\n" + document.DocumentElement!.OuterHtml;
    }
}
