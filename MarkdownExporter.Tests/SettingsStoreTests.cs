using MarkdownExporter.Conversion;
using MarkdownExporter.Localization;
using MarkdownExporter.Settings;
using MarkdownExporter.Themes;

namespace MarkdownExporter.Tests;

[TestFixture]
public sealed class SettingsStoreTests
{
    [Test]
    public void Load_MissingOrMalformedFile_UsesDefaults()
    {
        using var folder = new TestFolder();
        var path = folder.PathFor("settings.json");

        Assert.That(SettingsStore.Load(path).Defaults.Format, Is.EqualTo(ExportFormat.Both));

        folder.Write("settings.json", "{invalid json");
        Assert.That(SettingsStore.Load(path).Defaults.Format, Is.EqualTo(ExportFormat.Both));
    }

    [Test]
    public void Load_NullDefaults_RepairsSettings()
    {
        using var folder = new TestFolder();
        var path = folder.Write("settings.json", "{\"Defaults\":null}");

        Assert.That(SettingsStore.Load(path).Defaults, Is.Not.Null);
    }

    [Test]
    public void Save_ThenLoad_RoundTripsAndReplacesExistingFile()
    {
        using var folder = new TestFolder();
        var path = folder.PathFor("nested/settings.json");
        var settings = new AppSettings
        {
            Theme = AppThemePreference.Dark,
            Language = LanguagePreference.English,
            Defaults = new ConversionOptions { Format = ExportFormat.Pdf, Template = "custom" }
        };

        SettingsStore.Save(settings, path);
        settings.Defaults.Template = "changed";
        SettingsStore.Save(settings, path);
        var loaded = SettingsStore.Load(path);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Theme, Is.EqualTo(AppThemePreference.Dark));
            Assert.That(loaded.Language, Is.EqualTo(LanguagePreference.English));
            Assert.That(loaded.Defaults.Format, Is.EqualTo(ExportFormat.Pdf));
            Assert.That(loaded.Defaults.Template, Is.EqualTo("changed"));
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        });
    }
}
