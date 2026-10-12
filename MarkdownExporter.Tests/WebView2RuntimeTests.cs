using MarkdownExporter.Conversion;
using Microsoft.Web.WebView2.Core;

namespace MarkdownExporter.Tests;

public sealed class WebView2RuntimeTests
{
    [Test]
    public void InstalledRuntimeIsAvailable()
    {
        Assert.That(WebView2Runtime.IsAvailable(() => "140.0.0.0"), Is.True);
    }

    [Test]
    public void MissingRuntimeIsUnavailable()
    {
        Assert.That(WebView2Runtime.IsAvailable(() => throw new WebView2RuntimeNotFoundException()), Is.False);
        Assert.That(WebView2Runtime.IsAvailable(() => ""), Is.False);
    }
}
