using System.Text.Json;
using System.Windows;
using MarkdownExporter.Localization;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace MarkdownExporter.Conversion;

public sealed record RenderOutput(string Html, IReadOnlyList<string> Warnings);

public sealed class WebViewRenderer : IDisposable
{
    private readonly WebView2 browser = new();
    private readonly Window window;
    private bool initialized;

    public WebViewRenderer()
    {
        // The WPF WebView2 has its own child window, so parent opacity alone cannot hide it.
        window = new Window
        {
            Width = 1000,
            Height = 750,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = SystemParameters.VirtualScreenLeft - 1100,
            Top = SystemParameters.VirtualScreenTop - 850,
            WindowStyle = WindowStyle.None,
            Opacity = 0,
            ShowInTaskbar = false,
            ShowActivated = false,
            Content = browser
        };
        window.Show();
    }

    public async Task<RenderOutput> RenderAsync(string stageHtml, string? pdfPath, PageOrientation orientation, bool fitFigures)
    {
        if (!initialized)
        {
            var profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MarkdownExporter", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profile);
            await browser.EnsureCoreWebView2Async(environment);
            initialized = true;
        }
        var navigation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Completed(object? _, CoreWebView2NavigationCompletedEventArgs e) => navigation.TrySetResult(e.IsSuccess);
        browser.CoreWebView2.NavigationCompleted += Completed;
        try
        {
            browser.CoreWebView2.Navigate(new Uri(stageHtml).AbsoluteUri);
            if (!await navigation.Task.WaitAsync(TimeSpan.FromSeconds(30)))
                throw new InvalidOperationException(LanguageManager.Text("NavigationFailed"));
        }
        finally { browser.CoreWebView2.NavigationCompleted -= Completed; }

        var bundle = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "WebAssets", "render-markdig.js"));
        await browser.CoreWebView2.ExecuteScriptAsync(bundle);
        await browser.CoreWebView2.ExecuteScriptAsync("""
            window.__mdexportState = 'pending';
            window.__mdexportImageWarnings = [];
            Promise.resolve(window.renderReady)
              .then(() => document.fonts.ready)
              .then(() => Promise.all(Array.from(document.images, image => image.decode().catch(error => {
                if (/^https?:/i.test(image.src)) { window.__mdexportImageWarnings.push(image.src); return; }
                throw error;
              }))))
              .then(() => window.__mdexportState = 'ready', error => window.__mdexportState = 'error: ' + error.message);
            """);
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            var state = JsonSerializer.Deserialize<string>(await browser.CoreWebView2.ExecuteScriptAsync("window.__mdexportState"));
            if (state == "ready")
                break;
            if (state?.StartsWith("error:", StringComparison.Ordinal) == true)
                throw new InvalidOperationException(LanguageManager.Format("RenderingFailed", state));
            await Task.Delay(100);
        }
        if (JsonSerializer.Deserialize<string>(await browser.CoreWebView2.ExecuteScriptAsync("window.__mdexportState")) != "ready")
            throw new TimeoutException(LanguageManager.Text("RenderTimeout"));
        var failedFonts = JsonSerializer.Deserialize<string[]>(await browser.CoreWebView2.ExecuteScriptAsync(
            "Array.from(document.fonts).filter(font => font.status === 'error').map(font => font.family)")) ?? [];
        if (failedFonts.Length > 0)
            throw new InvalidOperationException(LanguageManager.Format("FontsFailed", string.Join(", ", failedFonts)));
        var imageWarnings = JsonSerializer.Deserialize<string[]>(await browser.CoreWebView2.ExecuteScriptAsync("window.__mdexportImageWarnings")) ?? [];

        if (pdfPath is not null)
        {
            var landscape = orientation == PageOrientation.Landscape;
            var pageCss = landscape ? "@page{size:A4 landscape;margin:15mm}" : "@page{size:A4 portrait;margin:15mm}";
            if (fitFigures)
            {
                var maxWidth = landscape ? "267mm" : "180mm";
                var maxHeight = landscape ? "150mm" : "267mm";
                // Inline important rules keep a template's width/height when they fit, but
                // prevent its stronger selectors or minimum sizes from defeating the page limit.
                await browser.CoreWebView2.ExecuteScriptAsync($$"""
                    document.querySelectorAll('img,#diagram svg').forEach(figure => {
                      const style = figure.style;
                      style.setProperty('max-width', 'min(100%, {{maxWidth}})', 'important');
                      style.setProperty('max-height', '{{maxHeight}}', 'important');
                      style.setProperty('min-width', '0', 'important');
                      style.setProperty('min-height', '0', 'important');
                      style.setProperty('box-sizing', 'border-box', 'important');
                      style.setProperty('object-fit', 'contain', 'important');
                      style.setProperty('break-inside', 'avoid-page', 'important');
                    });
                    """);
            }
            await browser.CoreWebView2.ExecuteScriptAsync($"document.head.appendChild(Object.assign(document.createElement('style'), {{textContent: {JsonSerializer.Serialize(pageCss)}}}));");
            var settings = browser.CoreWebView2.Environment.CreatePrintSettings();
            settings.Orientation = landscape ? CoreWebView2PrintOrientation.Landscape : CoreWebView2PrintOrientation.Portrait;
            settings.PageWidth = 8.2677;
            settings.PageHeight = 11.6929;
            settings.MarginTop = settings.MarginBottom = settings.MarginLeft = settings.MarginRight = .5906;
            settings.ShouldPrintBackgrounds = true;
            settings.ShouldPrintHeaderAndFooter = false;
            if (!await browser.CoreWebView2.PrintToPdfAsync(pdfPath, settings))
                throw new InvalidOperationException(LanguageManager.Text("PdfFailed"));
        }
        var result = await browser.CoreWebView2.ExecuteScriptAsync("""
            (() => {
              const clone = document.documentElement.cloneNode(true);
              clone.querySelectorAll('script').forEach(x => x.remove());
              return '<!doctype html>\n' + clone.outerHTML;
            })()
            """);
        var html = JsonSerializer.Deserialize<string>(result) ?? throw new InvalidOperationException(LanguageManager.Text("HtmlCaptureFailed"));
        return new RenderOutput(html, [.. imageWarnings.Select(url => LanguageManager.Format("ExternalImageFailed", url))]);
    }

    public void Dispose()
    {
        browser.Dispose();
        window.Close();
    }
}
