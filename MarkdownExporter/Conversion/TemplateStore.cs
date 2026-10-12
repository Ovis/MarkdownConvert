using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using MarkdownExporter.Localization;

namespace MarkdownExporter.Conversion;

public sealed record TemplateDocument(string Directory, string Index, string Style);

public static class TemplateStore
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MarkdownExporter", "Template");

    public static void Initialize() => Initialize(Root, Path.Combine(AppContext.BaseDirectory, "Template", "default"));

    internal static void Initialize(string root, string source)
    {
        Directory.CreateDirectory(root);
        var target = Path.Combine(root, "default");
        if (Directory.Exists(target))
            return;
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException(LanguageManager.Format("BundledTemplateMissing", source));
        Directory.CreateDirectory(target);
        foreach (var sourceFile in Directory.GetFiles(source))
            File.Copy(sourceFile, Path.Combine(target, Path.GetFileName(sourceFile)), false);
    }

    public static TemplateDocument Load(string nameOrPath)
    {
        if (string.IsNullOrWhiteSpace(nameOrPath))
            throw new ArgumentException(LanguageManager.Text("TemplateNameEmpty"));
        var directory = Path.IsPathFullyQualified(nameOrPath)
            ? Path.GetFullPath(nameOrPath)
            : Path.Combine(Root, ValidateName(nameOrPath));
        var indexPath = Path.Combine(directory, "index.html");
        var stylePath = Path.Combine(directory, "style.css");
        if (!File.Exists(indexPath) || !File.Exists(stylePath))
            throw new FileNotFoundException(LanguageManager.Format("TemplateFilesMissing", directory));
        var index = File.ReadAllText(indexPath);
        var style = File.ReadAllText(stylePath);
        if (!index.Contains("{{content}}", StringComparison.Ordinal) || !index.Contains("{{style}}", StringComparison.Ordinal))
            throw new InvalidDataException(LanguageManager.Text("TemplatePlaceholdersMissing"));
        ValidateTemplate(index, style);
        return new TemplateDocument(directory, index, style);
    }

    private static void ValidateTemplate(string index, string style)
    {
        if (Regex.IsMatch(style, @"@import\b|expression\s*\(|javascript\s*:|<\s*/?\s*style\b|<\s*script\b", RegexOptions.IgnoreCase))
            throw new InvalidDataException(LanguageManager.Text("UnsafeTemplateCss"));
        var document = new HtmlParser().ParseDocument(index);
        if (document.QuerySelector("script,iframe,object,embed,link,base,meta[http-equiv]") is not null)
            throw new InvalidDataException(LanguageManager.Text("UnsafeTemplateElement"));
        foreach (var element in document.QuerySelectorAll("*"))
            foreach (var attribute in element.Attributes)
            {
                if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase) ||
                    attribute.Name.Equals("srcdoc", StringComparison.OrdinalIgnoreCase) ||
                    attribute.Value.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                    attribute.Value.TrimStart().StartsWith("data:text/html", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(LanguageManager.Text("UnsafeTemplateAttribute"));
            }
    }

    public static IEnumerable<string> Names() => Directory.Exists(Root)
        ? Directory.GetDirectories(Root).Select(Path.GetFileName).Where(x => x is not null).Select(x => x!)
        : ["default"];

    private static string ValidateName(string name)
    {
        if (name is "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\'))
            throw new ArgumentException(LanguageManager.Text("InvalidTemplateName"));
        return name;
    }
}
