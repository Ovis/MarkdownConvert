using AngleSharp.Html.Parser;
using MarkdownExporter.Localization;

namespace MarkdownExporter.Conversion;

internal static class MarkdownLinkRewriter
{
    public static string Rewrite(string html, string sourcePath, HashSet<string> successful,
        List<string> warnings, bool reportWarnings,
        IReadOnlyDictionary<string, string>? targetPaths = null, string? outputDirectory = null)
    {
        if (!html.Contains("data-mdexport-original", StringComparison.Ordinal))
            return html;
        var document = new HtmlParser().ParseDocument(html);
        foreach (var link in document.QuerySelectorAll("a[data-mdexport-original]"))
        {
            var original = link.GetAttribute("data-mdexport-original")!;
            if (!LinkedMarkdownLinks.TryResolve(Path.GetDirectoryName(sourcePath)!, original, out var target))
            {
                link.RemoveAttribute("data-mdexport-original");
                continue;
            }
            if (!successful.Contains(target))
            {
                link.SetAttribute("href", original);
                if (reportWarnings)
                    warnings.Add(LanguageManager.Format("LinkTargetFailed", original));
            }
            else if (targetPaths is not null && targetPaths.TryGetValue(target, out var destination))
            {
                var suffix = MarkdownDocumentBuilder.SplitUrl(original).Suffix;
                link.SetAttribute("href", MarkdownDocumentBuilder.RelativeUrl(outputDirectory!, destination) + suffix);
            }
            link.RemoveAttribute("data-mdexport-original");
        }
        return "<!doctype html>\n" + document.DocumentElement!.OuterHtml;
    }
}
