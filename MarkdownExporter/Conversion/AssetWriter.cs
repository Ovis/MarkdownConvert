using System.Security.Cryptography;
using MarkdownExporter.Localization;

namespace MarkdownExporter.Conversion;

public sealed class AssetWriter(string outputDirectory, string stem)
{
    private readonly string root = Path.Combine(outputDirectory, stem + "_assets");
    private readonly List<string> created = [];
    public string Root => root;

    public string Store(string source, string section, string? relativeName = null)
    {
        var name = relativeName ?? Path.GetFileName(source);
        if (Path.IsPathRooted(name) || name.Split('/', '\\').Any(p => p is "." or ".." or ""))
            throw new InvalidDataException(LanguageManager.Format("InvalidAssetPath", name));
        var target = Path.GetFullPath(Path.Combine(root, section, name.Replace('/', Path.DirectorySeparatorChar)));
        if (!target.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(LanguageManager.Format("InvalidAssetPath", name));
        var bytes = File.ReadAllBytes(source);
        if (File.Exists(target) && !File.ReadAllBytes(target).AsSpan().SequenceEqual(bytes))
        {
            var ext = Path.GetExtension(target);
            var hash = Convert.ToHexString(SHA256.HashData(bytes))[..12].ToLowerInvariant();
            target = Path.Combine(Path.GetDirectoryName(target)!, Path.GetFileNameWithoutExtension(target) + "-" + hash + ext);
        }
        if (!File.Exists(target))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, bytes);
            created.Add(target);
        }
        return Path.GetRelativePath(outputDirectory, target).Replace('\\', '/');
    }

    public void CleanCreated()
    {
        foreach (var file in created)
            if (File.Exists(file))
                File.Delete(file);
        foreach (var file in created)
        {
            var directory = Path.GetDirectoryName(file);
            while (directory is not null && directory.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
                if (directory.Equals(root, StringComparison.OrdinalIgnoreCase))
                    break;
                directory = Path.GetDirectoryName(directory);
            }
        }
    }
}
