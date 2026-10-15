namespace MarkdownExporter.Conversion;

internal sealed record LinkedMarkdownLink(string Url, string TargetPath);

internal static class LinkedMarkdownLinks
{
    public static IReadOnlyList<LinkedMarkdownLink> Find(string sourcePath, string markdown)
    {
        var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;
        var document = MarkdownDocumentBuilder.ParseMarkdown(markdown);
        var links = new List<LinkedMarkdownLink>();
        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var url = anchor.GetAttribute("href")!;
            if (TryResolve(sourceDirectory, url, out var target))
                links.Add(new LinkedMarkdownLink(url, target));
        }
        return links;
    }

    public static bool TryResolve(string sourceDirectory, string url, out string target)
    {
        target = "";
        if (url.StartsWith('#') || url.StartsWith("//") || Uri.TryCreate(url, UriKind.Absolute, out _))
            return false;
        var end = url.IndexOfAny(['?', '#']);
        var part = end < 0 ? url : url[..end];
        if (!part.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
            !part.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
            return false;
        try
        {
            var relative = Uri.UnescapeDataString(part).Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(relative))
                return false;
            target = Path.GetFullPath(Path.Combine(sourceDirectory, relative));
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UriFormatException)
        {
            return false;
        }
    }
}
