namespace MarkdownExporter.Conversion;

internal sealed record PlannedInput(InputFile Input, string OutputDirectory, string Stem)
{
    public string Path => Input.Path;
    public string HtmlPath => System.IO.Path.Combine(OutputDirectory, Stem + ".html");
    public string PdfPath => System.IO.Path.Combine(OutputDirectory, Stem + ".pdf");
}

internal sealed class ConversionPlan
{
    private ConversionPlan(IReadOnlyList<PlannedInput> documents,
        IReadOnlyDictionary<string, IReadOnlyList<string>> missingLinks)
    {
        Documents = documents;
        MissingLinks = missingLinks;
    }

    public IReadOnlyList<PlannedInput> Documents { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> MissingLinks { get; }

    public static ConversionPlan Create(IReadOnlyList<InputFile> inputs, ConversionOptions options)
    {
        var discovered = new List<InputFile>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputs)
        {
            var path = System.IO.Path.GetFullPath(input.Path);
            if (seen.Add(path))
                discovered.Add(input with { Path = path });
        }

        var missing = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (options.FollowMarkdownLinks)
            for (var index = 0; index < discovered.Count; index++)
            {
                var input = discovered[index];
                if (!File.Exists(input.Path) || !ConversionService.IsMarkdown(input.Path))
                    continue;
                try
                {
                    var links = LinkedMarkdownLinks.Find(input.Path, EncodingReader.Read(input.Path, input.Encoding));
                    var unavailable = new List<string>();
                    foreach (var link in links)
                    {
                        if (!File.Exists(link.TargetPath))
                        {
                            unavailable.Add(link.Url);
                            continue;
                        }
                        if (seen.Add(link.TargetPath))
                            discovered.Add(new InputFile(link.TargetPath, options.Encoding));
                    }
                    if (unavailable.Count > 0)
                        missing[input.Path] = unavailable;
                }
                // The conversion itself reports unreadable or malformed inputs.
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
                {
                }
            }

        var candidates = discovered.Select(input =>
        {
            var directory = options.OutputDirectory is null
                ? System.IO.Path.GetDirectoryName(input.Path)!
                : System.IO.Path.GetFullPath(options.OutputDirectory);
            return new Candidate(input, directory, System.IO.Path.GetFileNameWithoutExtension(input.Path));
        }).ToArray();

        // Reserve every original stem before suffixing duplicates. A generated "intro-2"
        // must not steal the name of a real intro-2.md.
        foreach (var group in candidates.GroupBy(x => x.OutputDirectory, StringComparer.OrdinalIgnoreCase))
        {
            var reserved = group.Select(x => x.Stem).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var candidate in group)
            {
                if (used.Add(candidate.Stem))
                    continue;
                for (var number = 2; ; number++)
                {
                    var stem = candidate.Stem + "-" + number;
                    if (reserved.Contains(stem) || !used.Add(stem))
                        continue;
                    candidate.Stem = stem;
                    break;
                }
            }
        }

        return new ConversionPlan(candidates.Select(x => new PlannedInput(x.Input, x.OutputDirectory, x.Stem)).ToArray(), missing);
    }

    private sealed class Candidate(InputFile input, string outputDirectory, string stem)
    {
        public InputFile Input { get; } = input;
        public string OutputDirectory { get; } = outputDirectory;
        public string Stem { get; set; } = stem;
    }
}
