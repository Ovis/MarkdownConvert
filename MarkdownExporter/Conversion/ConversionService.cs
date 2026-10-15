using System.Text;
using MarkdownExporter.Localization;

namespace MarkdownExporter.Conversion;

public enum PreflightIssueKind { MissingInput, InvalidMarkdown, DuplicateOutput, OutputExists }

public sealed record PreflightIssue(PreflightIssueKind Kind, string Path)
{
    public string Message => LanguageManager.Format(Kind switch
    {
        PreflightIssueKind.MissingInput => "InputMissing",
        PreflightIssueKind.InvalidMarkdown => "InvalidMarkdown",
        PreflightIssueKind.DuplicateOutput => "DuplicateOutput",
        _ => "OutputExists"
    }, Path);
}

public sealed class ConversionService
{
    private readonly MarkdownDocumentBuilder builder = new();

    public static IReadOnlyList<PreflightIssue> Preflight(IReadOnlyList<InputFile> inputs, ConversionOptions options)
    {
        var errors = new List<PreflightIssue>();
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputs)
        {
            var path = Path.GetFullPath(input.Path);
            if (!File.Exists(path))
                errors.Add(new(PreflightIssueKind.MissingInput, path));
            if (!IsMarkdown(path))
                errors.Add(new(PreflightIssueKind.InvalidMarkdown, path));
            var folder = options.OutputDirectory is null ? Path.GetDirectoryName(path)! : Path.GetFullPath(options.OutputDirectory);
            var stem = Path.GetFileNameWithoutExtension(path);
            foreach (var format in options.Format switch
            {
                ExportFormat.Html => new[] { ".html" },
                ExportFormat.Pdf => [".pdf"],
                _ => [".html", ".pdf"]
            })
            {
                var target = Path.Combine(folder, stem + format);
                if (!targets.Add(target))
                    errors.Add(new(PreflightIssueKind.DuplicateOutput, target));
                if (File.Exists(target) && !options.Overwrite)
                    errors.Add(new(PreflightIssueKind.OutputExists, target));
            }
        }
        return errors;
    }

    public static bool IsMarkdown(string path) => Path.GetExtension(path).ToLowerInvariant() is ".md" or ".markdown";

    public async Task<IReadOnlyList<ConversionResult>> ConvertAsync(IReadOnlyList<InputFile> inputs,
        ConversionOptions options, Action<ConversionResult>? onResult = null,
        Action<string, string>? onProgress = null)
    {
        var collisions = Preflight(inputs, options)
            .Where(x => x.Kind == PreflightIssueKind.DuplicateOutput).ToArray();
        if (collisions.Length > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, collisions.Select(x => x.Message)));
        WebView2Runtime.EnsureAvailable();
        TemplateStore.Initialize();
        var template = TemplateStore.Load(options.Template);
        var htmlWanted = options.Format is ExportFormat.Html or ExportFormat.Both;
        var pdfWanted = options.Format is ExportFormat.Pdf or ExportFormat.Both;
        var htmlTargets = (htmlWanted ? inputs : []).ToDictionary(x => Path.GetFullPath(x.Path), x =>
        {
            var folder = options.OutputDirectory is null ? Path.GetDirectoryName(Path.GetFullPath(x.Path))! : Path.GetFullPath(options.OutputDirectory);
            return Path.Combine(folder, Path.GetFileNameWithoutExtension(x.Path) + ".html");
        }, StringComparer.OrdinalIgnoreCase);
        var pending = new List<PendingDocument>();
        using var renderer = new WebViewRenderer();
        try
        {
            // First determine which HTML files can actually be produced.
            foreach (var input in inputs)
            {
                var path = Path.GetFullPath(input.Path);
                var outputDir = options.OutputDirectory is null ? Path.GetDirectoryName(path)! : Path.GetFullPath(options.OutputDirectory);
                var stem = Path.GetFileNameWithoutExtension(path);
                var entry = new PendingDocument(path, outputDir, stem);
                pending.Add(entry);
                try
                {
                    if (!File.Exists(path))
                        throw new FileNotFoundException(LanguageManager.Format("InputMissing", path));
                    if (!IsMarkdown(path))
                        throw new InvalidDataException(LanguageManager.Format("InvalidMarkdown", path));
                    if (!options.Overwrite && ((htmlWanted && File.Exists(entry.HtmlPath)) || (pdfWanted && File.Exists(entry.PdfPath))))
                        throw new IOException(LanguageManager.Format("OutputExists", htmlWanted && File.Exists(entry.HtmlPath) ? entry.HtmlPath : entry.PdfPath));
                    Directory.CreateDirectory(outputDir);
                    var markdown = EncodingReader.Read(path, input.Encoding);
                    entry.Assets = new AssetWriter(outputDir, stem);
                    var html = builder.Build(path, markdown, outputDir, stem, options, template, entry.Assets, htmlTargets);
                    File.WriteAllText(entry.StagePath, html, new UTF8Encoding(false));
                    var rendered = await renderer.RenderAsync(entry.StagePath, null, options.Orientation, options.FitFiguresToPage);
                    entry.Warnings.AddRange(rendered.Warnings);
                    if (htmlWanted)
                    {
                        WriteAtomically(entry.HtmlPath, rendered.Html);
                        entry.HtmlSuccess = true;
                    }
                    entry.Ready = true;
                    onProgress?.Invoke(path, htmlWanted ? "HtmlDone" : "RenderDone");
                }
                catch (Exception ex) { entry.Error = ex.Message; onProgress?.Invoke(path, "Failure"); }
            }

            if (htmlWanted)
                FinalizeMarkdownLinks(pending);

            // Print after link targets are known; the PDF sees the same final links as HTML.
            if (pdfWanted)
                foreach (var entry in pending.Where(x => x.Ready && x.Error is null))
                {
                    var temporaryPdf = entry.StagePath + ".pdf";
                    var portablePdf = entry.StagePath + ".portable.pdf";
                    try
                    {
                        var rendered = await renderer.RenderAsync(entry.StagePath, temporaryPdf, options.Orientation, options.FitFiguresToPage);
                        foreach (var warning in rendered.Warnings.Except(entry.Warnings))
                            entry.Warnings.Add(warning);
                        var completedPdf = await PdfLinkRewriter.RewriteLocalPdfLinksAsync(temporaryPdf, portablePdf, entry.PdfPath)
                            ? portablePdf : temporaryPdf;
                        File.Move(completedPdf, entry.PdfPath, true);
                        entry.PdfSuccess = true;
                        onProgress?.Invoke(entry.Path, "PdfDone");
                    }
                    catch (Exception ex) { entry.Error = ex.Message; onProgress?.Invoke(entry.Path, "Failure"); }
                    finally
                    {
                        if (File.Exists(temporaryPdf))
                            File.Delete(temporaryPdf);
                        if (File.Exists(portablePdf))
                            File.Delete(portablePdf);
                    }
                }
        }
        finally
        {
            foreach (var entry in pending)
            {
                if (File.Exists(entry.StagePath))
                    File.Delete(entry.StagePath);
                if (!entry.HtmlSuccess)
                    entry.Assets?.CleanCreated();
            }
        }

        var results = pending.Select(x => new ConversionResult(x.Path, x.HtmlSuccess, x.PdfSuccess, x.Error, x.Warnings)).ToArray();
        foreach (var result in results)
            onResult?.Invoke(result);
        return results;
    }

    private static void FinalizeMarkdownLinks(List<PendingDocument> pending)
    {
        var successful = pending.Where(x => x.HtmlSuccess).Select(x => x.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in pending.Where(x => x.HtmlSuccess))
        {
            try
            {
                var completedHtml = MarkdownLinkRewriter.Rewrite(File.ReadAllText(entry.HtmlPath), entry.Path, successful, entry.Warnings, true);
                WriteAtomically(entry.HtmlPath, completedHtml);
                var stageHtml = MarkdownLinkRewriter.Rewrite(File.ReadAllText(entry.StagePath), entry.Path, successful, entry.Warnings, false);
                WriteAtomically(entry.StagePath, stageHtml);
            }
            catch (Exception ex) { entry.Error = LanguageManager.Format("LinkFinalizeFailed", ex.Message); }
        }
    }

    private static void WriteAtomically(string path, string contents)
    {
        var temporary = path + ".mdexport-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, contents, new UTF8Encoding(false));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private sealed class PendingDocument(string path, string outputDirectory, string stem)
    {
        public string Path { get; } = path;
        public string HtmlPath { get; } = System.IO.Path.Combine(outputDirectory, stem + ".html");
        public string PdfPath { get; } = System.IO.Path.Combine(outputDirectory, stem + ".pdf");
        public string StagePath { get; } = System.IO.Path.Combine(outputDirectory, ".mdexport-" + Guid.NewGuid().ToString("N") + ".html");
        public AssetWriter? Assets
        {
            get; set;
        }
        public List<string> Warnings { get; } = [];
        public string? Error
        {
            get; set;
        }
        public bool Ready
        {
            get; set;
        }
        public bool HtmlSuccess
        {
            get; set;
        }
        public bool PdfSuccess
        {
            get; set;
        }
    }
}
