namespace MarkdownExporter.Tests;

internal sealed class TestFolder : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "MarkdownExporter.Tests", Guid.NewGuid().ToString("N"));

    public TestFolder() => Directory.CreateDirectory(Root);

    public string PathFor(string relativePath) => Path.GetFullPath(Path.Combine(Root, relativePath));

    public string Write(string relativePath, string content)
    {
        var path = PathFor(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public string Write(string relativePath, byte[] content)
    {
        var path = PathFor(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
            Directory.Delete(Root, true);
    }
}
