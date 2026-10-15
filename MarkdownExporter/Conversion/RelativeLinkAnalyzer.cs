namespace MarkdownExporter.Conversion;

internal sealed record RelativeLinkRisk(string Path, IReadOnlyList<string> Links);

internal static class RelativeLinkAnalyzer
{
    public static IReadOnlyList<RelativeLinkRisk> Scan(IReadOnlyList<InputFile> inputs, ConversionOptions options)
    {
        if (options.OutputDirectory is null)
            return [];

        var outputDirectory = Path.GetFullPath(options.OutputDirectory);
        var plan = ConversionPlan.Create(inputs, options);
        var convertedTargets = plan.Documents.Select(x => x.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var risks = new List<RelativeLinkRisk>();
        foreach (var input in plan.Documents)
        {
            var path = input.Path;
            if (Path.GetDirectoryName(path)!.Equals(outputDirectory, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                continue;

            try
            {
                var links = Find(path, EncodingReader.Read(path, input.Input.Encoding), outputDirectory, convertedTargets);
                if (links.Count > 0)
                    risks.Add(new RelativeLinkRisk(path, links));
            }
            // Conversion reports unreadable inputs separately; they cannot be inspected here.
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (InvalidDataException) { }
            catch (ArgumentException) { }
        }
        return risks;
    }

    public static IReadOnlyList<string> Find(string inputPath, string markdown, string outputDirectory,
        IReadOnlySet<string> htmlTargets)
    {
        var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(inputPath))!;
        outputDirectory = Path.GetFullPath(outputDirectory);
        if (sourceDirectory.Equals(outputDirectory, StringComparison.OrdinalIgnoreCase))
            return [];

        var document = MarkdownDocumentBuilder.ParseMarkdown(markdown);
        var affected = new List<string>();
        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var url = anchor.GetAttribute("href")!;
            if (url.StartsWith('#') || url.StartsWith("//") || Uri.TryCreate(url, UriKind.Absolute, out _))
                continue;

            var end = url.IndexOfAny(['?', '#']);
            var pathPart = end < 0 ? url : url[..end];
            if (pathPart.Length == 0)
                continue;

            try
            {
                var relative = Uri.UnescapeDataString(pathPart).Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(relative))
                    continue;

                var sourceTarget = Path.GetFullPath(Path.Combine(sourceDirectory, relative));
                var outputTarget = Path.GetFullPath(Path.Combine(outputDirectory, relative));
                if (sourceTarget.Equals(outputTarget, StringComparison.OrdinalIgnoreCase) ||
                    htmlTargets.Contains(sourceTarget))
                    continue;
                affected.Add(url);
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
            catch (PathTooLongException) { }
            catch (UriFormatException) { }
        }
        return affected;
    }
}
