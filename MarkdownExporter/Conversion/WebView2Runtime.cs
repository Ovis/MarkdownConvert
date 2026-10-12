using MarkdownExporter.Localization;
using Microsoft.Web.WebView2.Core;

namespace MarkdownExporter.Conversion;

public static class WebView2Runtime
{
    public const string DownloadUrl = "https://developer.microsoft.com/microsoft-edge/webview2/";

    public static bool IsAvailable() => IsAvailable(() => CoreWebView2Environment.GetAvailableBrowserVersionString());

    internal static bool IsAvailable(Func<string> versionProbe)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(versionProbe());
        }
        catch (WebView2RuntimeNotFoundException)
        {
            return false;
        }
    }

    public static void EnsureAvailable()
    {
        if (!IsAvailable())
            throw new InvalidOperationException(LanguageManager.Format("WebView2Missing", DownloadUrl));
    }
}
