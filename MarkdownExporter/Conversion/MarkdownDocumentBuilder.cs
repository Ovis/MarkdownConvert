using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;
using Markdig;
using Markdown.ColorCode;
using MarkdownExporter.Localization;

namespace MarkdownExporter.Conversion;

public sealed class MarkdownDocumentBuilder
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions().UseColorCode(HtmlFormatterType.Css).Build();
    private static readonly Regex CssUrl = new(@"url\(\s*(?<quote>['""']?)(?<url>[^)'""']+)\k<quote>\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Build(string inputPath, string markdown, string outputDirectory, string stem,
        ConversionOptions options, TemplateDocument template, AssetWriter assets,
        IReadOnlyDictionary<string, string> htmlTargets)
    {
        var document = ParseMarkdown(markdown);
        var sourceDirectory = Path.GetDirectoryName(inputPath)!;
        // Validate every local image before creating any output assets.
        foreach (var image in document.QuerySelectorAll("img[src]"))
        {
            var url = image.GetAttribute("src")!;
            if (!IsLocal(url))
                continue;
            var local = Resolve(sourceDirectory, url);
            if (!IsImage(local))
                throw new InvalidDataException(LanguageManager.Format("UnsupportedImage", url));
            if (!File.Exists(local))
                throw new FileNotFoundException(LanguageManager.Format("ImageMissing", url));
        }
        foreach (var image in document.QuerySelectorAll("img[src]"))
        {
            var url = image.GetAttribute("src")!;
            if (!IsLocal(url))
                continue;
            var local = Resolve(sourceDirectory, url);
            image.SetAttribute("src", options.EmbedImages ? ToDataUri(local) : assets.Store(local, "content"));
        }
        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var url = anchor.GetAttribute("href")!;
            if (!IsLocal(url))
                continue;
            var split = SplitUrl(url);
            if (!split.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
                !split.Path.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
                continue;
            var local = Resolve(sourceDirectory, url);
            if (htmlTargets.TryGetValue(local, out var html))
            {
                anchor.SetAttribute("href", RelativeUrl(outputDirectory, html) + split.Suffix);
                anchor.SetAttribute("data-mdexport-original", url);
            }
        }
        var content = document.Body!.InnerHtml;
        var title = document.QuerySelector("h1")?.TextContent.Trim();
        if (string.IsNullOrWhiteSpace(title))
            title = stem;
        var index = template.Index;
        var css = template.Style;
        var templateAssets = Path.Combine(template.Directory, "assets");
        if (Directory.Exists(templateAssets))
        {
            index = RewriteTemplateHtml(index, templateAssets, assets, options.EmbedImages);
            css = CssUrl.Replace(css, match =>
            {
                var url = match.Groups["url"].Value;
                if (!IsLocal(url))
                    return match.Value;
                var file = Resolve(template.Directory, url);
                if (!file.StartsWith(Path.GetFullPath(templateAssets) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(LanguageManager.Format("TemplateAssetsOnly", url));
                if (!File.Exists(file))
                    throw new FileNotFoundException(LanguageManager.Format("TemplateAssetMissing", url));
                var newUrl = options.EmbedImages && IsImage(file)
                    ? ToDataUri(file) : assets.Store(file, "template", Path.GetRelativePath(templateAssets, file));
                return "url('" + newUrl + "')";
            });
        }
        else if (Regex.IsMatch(index + css, @"assets/", RegexOptions.IgnoreCase))
            throw new DirectoryNotFoundException(LanguageManager.Format("TemplateAssetsMissing", templateAssets));

        // The explicit page break is an application feature, independent of custom CSS.
        css += "\n.pagebreak{display:none}@media print{.pagebreak{display:block;break-before:page}}";

        if (content.Contains("class=\"math\"", StringComparison.Ordinal) || content.Contains("class=\"mermaid\"", StringComparison.Ordinal))
        {
            var webAssets = Path.Combine(AppContext.BaseDirectory, "WebAssets");
            var katexCss = File.ReadAllText(Path.Combine(webAssets, "katex.min.css"));
            katexCss = Regex.Replace(katexCss, @"url\((['""']?)(fonts/[^)'""']+)\1\)", match =>
            {
                var font = Path.Combine(webAssets, match.Groups[2].Value.Replace('/', Path.DirectorySeparatorChar));
                var url = assets.Store(font, "template", match.Groups[2].Value);
                return "url('" + url + "')";
            });
            assets.Store(Path.Combine(AppContext.BaseDirectory, "Licence", "KaTeX-LICENSE.txt"), "template", "fonts/LICENSE-KaTeX.txt");
            css += "\n" + katexCss;
        }
        var htmlDocument = index.Replace("{{content}}", content, StringComparison.Ordinal)
            .Replace("{{style}}", css, StringComparison.Ordinal)
            .Replace("{{title}}", WebUtility.HtmlEncode(title), StringComparison.Ordinal);
        return htmlDocument;
    }

    internal static IHtmlDocument ParseMarkdown(string markdown)
    {
        markdown = Regex.Replace(markdown, @"(?m)^\s*<!--\s*pagebreak\s*-->\s*$", "<div class=\"pagebreak\"></div>", RegexOptions.IgnoreCase);
        var raw = Markdig.Markdown.ToHtml(markdown, Pipeline);
        var sanitizer = CreateSanitizer();
        var safe = sanitizer.Sanitize(raw);
        var parser = new HtmlParser();
        return parser.ParseDocument("<body>" + safe + "</body>");
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var s = new HtmlSanitizer();
        s.AllowedTags.Clear();
        foreach (var tag in new[] { "a", "blockquote", "br", "code", "dd", "del", "details", "div", "dl", "dt", "em", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "img", "input", "li", "ol", "p", "pre", "s", "span", "strong", "sub", "summary", "sup", "table", "tbody", "td", "th", "thead", "tr", "ul" })
            s.AllowedTags.Add(tag);
        s.AllowedAttributes.Clear();
        foreach (var attr in new[] { "alt", "checked", "class", "colspan", "disabled", "height", "href", "id", "open", "rowspan", "src", "start", "title", "type", "width" })
            s.AllowedAttributes.Add(attr);
        s.AllowedCssProperties.Clear();
        s.AllowedAtRules.Clear();
        s.AllowedSchemes.Clear();
        foreach (var scheme in new[] { "http", "https", "mailto" })
            s.AllowedSchemes.Add(scheme);
        return s;
    }

    private static string RewriteTemplateHtml(string index, string assetsDir, AssetWriter assets, bool embedImages)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(index);
        foreach (var element in document.QuerySelectorAll("[src], [href]"))
            foreach (var attr in new[] { "src", "href" })
            {
                var url = element.GetAttribute(attr);
                if (url is null || !IsLocal(url) || !url.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
                    continue;
                var file = Resolve(Path.GetDirectoryName(assetsDir)!, url);
                if (!file.StartsWith(Path.GetFullPath(assetsDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(LanguageManager.Format("InvalidTemplateAssetPath", url));
                if (!File.Exists(file))
                    throw new FileNotFoundException(LanguageManager.Format("TemplateAssetMissing", url));
                element.SetAttribute(attr, embedImages && IsImage(file)
                    ? ToDataUri(file) : assets.Store(file, "template", Path.GetRelativePath(assetsDir, file)));
            }
        return "<!doctype html>\n" + document.DocumentElement!.OuterHtml;
    }

    private static bool IsLocal(string url) => !url.StartsWith('#') && !url.StartsWith("//") &&
        !Uri.TryCreate(url, UriKind.Absolute, out _);
    private static (string Path, string Suffix) SplitUrl(string url)
    {
        var split = url.IndexOfAny(['?', '#']);
        return split < 0 ? (url, "") : (url[..split], url[split..]);
    }
    private static string Resolve(string directory, string url)
    {
        var path = Uri.UnescapeDataString(SplitUrl(url).Path).Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(directory, path));
    }
    private static string RelativeUrl(string fromDirectory, string destination)
    {
        var relative = Path.GetRelativePath(fromDirectory, destination);
        return string.Join("/", relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Select(segment => segment is "." or ".." ? segment : Uri.EscapeDataString(segment)));
    }
    private static bool IsImage(string path) => Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg" or ".bmp" or ".avif" or ".ico" or ".apng";
    private static string ToDataUri(string path)
    {
        var mime = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".bmp" => "image/bmp",
            ".avif" => "image/avif",
            ".ico" => "image/x-icon",
            ".apng" => "image/apng",
            _ => throw new InvalidDataException(LanguageManager.Format("UnknownImageType", path))
        };
        return $"data:{mime};base64,{Convert.ToBase64String(File.ReadAllBytes(path))}";
    }
}
