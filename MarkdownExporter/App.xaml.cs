using System.Text;
using System.Windows;
using MarkdownExporter.Conversion;
using MarkdownExporter.Dialogs;
using MarkdownExporter.Localization;
using MarkdownExporter.Settings;
using MarkdownExporter.Themes;

namespace MarkdownExporter;

public partial class App : Application
{
    public AppThemeManager ThemeManager { get; private set; } = null!;
    public AppSettings Settings { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Console.OutputEncoding = Encoding.UTF8;
        Settings = SettingsStore.Load();
        LanguageManager.Instance.SetPreference(Settings.Language);
        CommandLine command;
        try
        {
            command = CommandLineParser.Parse(e.Args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(CommandLineParser.Usage);
            Shutdown(2);
            return;
        }
        if (command.Help)
        {
            Console.WriteLine(CommandLineParser.Usage);
            Shutdown(0);
            return;
        }
        ThemeManager = new AppThemeManager(this);
        ThemeManager.SetPreference(Settings.Theme);
        try
        {
            TemplateStore.Initialize();
        }
        catch (Exception ex)
        {
            if (command.Run)
            {
                Console.Error.WriteLine(ex.Message);
                Shutdown(1);
                return;
            }
            // The startup dialog is shown before the main window exists.
            // Closing it must not trigger WPF's last-window shutdown.
            var previousShutdownMode = ShutdownMode;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                AppDialog.ShowMessage(null, ex.Message, "MarkdownExporter");
            }
            finally
            {
                ShutdownMode = previousShutdownMode;
            }
        }
        if (command.Run)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Dispatcher.BeginInvoke(async () => await RunCliAsync(command));
            return;
        }
        var main = new MainWindow(command.Files);
        MainWindow = main;
        main.Show();
        if (!WebView2Runtime.IsAvailable())
            Dispatcher.BeginInvoke(main.ShowWebView2Missing);
    }

    private async Task RunCliAsync(CommandLine command)
    {
        var code = 0;
        try
        {
            var inputs = command.Files.Select(p => new InputFile(p, command.Options.Encoding)).ToArray();
            var results = await new ConversionService().ConvertAsync(inputs, command.Options, result =>
            {
                Console.WriteLine($"{result.Path}: HTML={Status(result.HtmlSuccess)} PDF={Status(result.PdfSuccess)}");
                foreach (var warning in result.Warnings)
                    Console.Error.WriteLine(LanguageManager.Format("CliWarningPrefix", warning));
                if (result.Error is not null)
                    Console.Error.WriteLine(LanguageManager.Format("CliErrorPrefix", result.Error));
            });
            if (results.Any(r => !r.Success))
                code = 1;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); code = 1; }
        Shutdown(code);
    }

    private static string Status(bool success) => success ? LanguageManager.Text("Success") : "-";

    protected override void OnExit(ExitEventArgs e)
    {
        ThemeManager?.Dispose();
        base.OnExit(e);
    }
}
