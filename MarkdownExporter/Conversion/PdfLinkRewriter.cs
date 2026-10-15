using System.Diagnostics;
using System.Text.Json.Nodes;
using MarkdownExporter.Localization;

namespace MarkdownExporter.Conversion;

internal static class PdfLinkRewriter
{
    public static async Task<bool> RewriteLocalPdfLinksAsync(
        string inputPath, string outputPath, string finalPath, string? qpdfPath = null,
        IReadOnlyDictionary<string, string>? originalMarkdownLinks = null)
    {
        qpdfPath ??= Path.Combine(AppContext.BaseDirectory, "Tools", "qpdf", "qpdf.exe");
        if (!File.Exists(qpdfPath))
            throw new FileNotFoundException(LanguageManager.Format("QpdfMissing", qpdfPath), qpdfPath);

        var jsonPath = Path.Combine(Path.GetTempPath(), $"mdexport-qpdf-{Guid.NewGuid():N}.json");
        var patchPath = Path.Combine(Path.GetTempPath(), $"mdexport-qpdf-{Guid.NewGuid():N}.json");
        try
        {
            await RunQpdfAsync(qpdfPath, "--json-output", inputPath, jsonPath, "--json-stream-data=none");
            var root = JsonNode.Parse(await File.ReadAllTextAsync(jsonPath))
                ?? throw new InvalidDataException(LanguageManager.Text("QpdfInvalidJson"));
            var qpdf = root["qpdf"]?.AsArray();
            var objects = qpdf is { Count: > 1 } ? qpdf[1]?.AsObject() : null;
            if (objects is null)
                throw new InvalidDataException(LanguageManager.Text("QpdfInvalidJson"));

            var changedObjects = new JsonObject();
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(finalPath))!;
            foreach (var (key, entry) in objects)
            {
                if (!key.StartsWith("obj:", StringComparison.Ordinal) || entry is not JsonObject original)
                    continue;
                var copy = (JsonObject)original.DeepClone();
                if (RewriteLinks(copy["value"], outputDirectory, objects, changedObjects, originalMarkdownLinks))
                    changedObjects[key] = copy;
            }
            if (changedObjects.Count == 0)
                return false;

            var patch = new JsonObject
            {
                ["qpdf"] = new JsonArray(new JsonObject { ["jsonversion"] = 2 }, changedObjects)
            };
            await File.WriteAllTextAsync(patchPath, patch.ToJsonString());
            await RunQpdfAsync(qpdfPath, inputPath, $"--update-from-json={patchPath}", outputPath);
            return true;
        }
        finally
        {
            File.Delete(jsonPath);
            File.Delete(patchPath);
        }
    }

    private static bool RewriteLinks(JsonNode? node, string outputDirectory,
        JsonObject objects, JsonObject changedObjects,
        IReadOnlyDictionary<string, string>? originalMarkdownLinks)
    {
        if (node is JsonArray array)
        {
            var changed = false;
            foreach (var child in array)
                changed = RewriteLinks(child, outputDirectory, objects, changedObjects, originalMarkdownLinks) || changed;
            return changed;
        }
        if (node is not JsonObject dictionary)
            return false;

        var changedHere = false;
        if (dictionary["/Subtype"]?.GetValue<string>() == "/Link")
        {
            if (dictionary["/A"] is JsonObject action)
                changedHere = RewriteAction(action, outputDirectory, originalMarkdownLinks);
            else if (dictionary["/A"] is JsonValue reference && reference.TryGetValue<string>(out var value))
            {
                var objectKey = "obj:" + value;
                if (objects[objectKey] is JsonObject original)
                {
                    var actionObject = (JsonObject)(changedObjects[objectKey] ?? original).DeepClone();
                    if (actionObject["value"] is JsonObject referencedAction &&
                        RewriteAction(referencedAction, outputDirectory, originalMarkdownLinks))
                        changedObjects[objectKey] = actionObject;
                }
            }
        }
        foreach (var (_, child) in dictionary)
            changedHere = RewriteLinks(child, outputDirectory, objects, changedObjects, originalMarkdownLinks) || changedHere;
        return changedHere;
    }

    private static bool RewriteAction(JsonObject action, string outputDirectory,
        IReadOnlyDictionary<string, string>? originalMarkdownLinks)
    {
        if (action["/S"]?.GetValue<string>() != "/URI")
            return false;
        var value = action["/URI"]?.GetValue<string>();
        if (value is null || !value.StartsWith("u:", StringComparison.Ordinal) ||
            !Uri.TryCreate(value[2..], UriKind.Absolute, out var uri) || !uri.IsFile)
            return false;

        if (originalMarkdownLinks is not null &&
            originalMarkdownLinks.TryGetValue(uri.AbsoluteUri, out var original))
        {
            action["/URI"] = "u:" + original;
            return true;
        }
        if (!Path.GetExtension(uri.LocalPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return false;

        var relative = Path.GetRelativePath(outputDirectory, uri.LocalPath).Replace('\\', '/');
        if (Path.IsPathFullyQualified(relative))
            throw new InvalidOperationException(LanguageManager.Format("PdfLinkDifferentVolume", uri.LocalPath));
        if (uri.Query.Length == 0 && TryGetPageIndex(uri.Fragment, out var pageIndex))
        {
            action.Remove("/URI");
            action["/S"] = "/GoToR";
            action["/F"] = "u:" + relative;
            action["/D"] = new JsonArray(pageIndex, "/Fit");
            action["/NewWindow"] = true;
        }
        else
        {
            var escaped = string.Join('/', relative.Split('/').Select(segment =>
                segment is "." or ".." ? segment : Uri.EscapeDataString(segment)));
            action["/URI"] = "u:" + escaped + uri.Query + uri.Fragment;
        }
        return true;
    }

    private static async Task RunQpdfAsync(string executablePath, params string[] arguments)
    {
        var startInfo = CreateStartInfo(executablePath, arguments);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(LanguageManager.Text("QpdfStartFailed"));
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await stdout;
        var error = await stderr;
        if (process.ExitCode != 0)
            throw new InvalidOperationException(LanguageManager.Format(
                "QpdfFailed", process.ExitCode, string.IsNullOrWhiteSpace(error) ? output.Trim() : error.Trim()));
    }

    internal static ProcessStartInfo CreateStartInfo(string executablePath, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        return startInfo;
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
