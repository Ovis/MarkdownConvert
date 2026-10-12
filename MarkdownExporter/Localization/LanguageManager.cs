using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace MarkdownExporter.Localization;

public enum LanguagePreference
{
    System,
    Japanese,
    English
}

public sealed class LanguageManager : INotifyPropertyChanged
{
    private static readonly ResourceManager Resources = new(
        "MarkdownExporter.Localization.Strings", typeof(LanguageManager).Assembly);
    private static readonly CultureInfo Japanese = CultureInfo.GetCultureInfo("ja-JP");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private readonly CultureInfo systemCulture = CultureInfo.CurrentUICulture;
    private CultureInfo culture = Japanese;

    public static LanguageManager Instance { get; } = new();
    public LanguagePreference Preference { get; private set; }
    public CultureInfo Culture => culture;
    public string this[string key] => Resources.GetString(key, culture)
        ?? throw new MissingManifestResourceException($"Missing translation: {key}");

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public void SetPreference(LanguagePreference preference)
    {
        if (!Enum.IsDefined(preference))
            preference = LanguagePreference.System;

        var selected = preference switch
        {
            LanguagePreference.Japanese => Japanese,
            LanguagePreference.English => English,
            _ => systemCulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase)
                ? Japanese : English
        };
        if (Preference == preference && culture.Equals(selected))
            return;

        Preference = preference;
        culture = selected;
        CultureInfo.CurrentUICulture = selected;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public static string Text(string key) => Instance[key];
    public static string Format(string key, params object?[] args) =>
        string.Format(Instance.culture, Instance[key], args);
}
