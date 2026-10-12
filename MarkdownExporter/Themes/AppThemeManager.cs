using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace MarkdownExporter.Themes;

public enum AppThemePreference
{
    System,
    Light,
    Dark
}

/// <summary>Swaps only the palette. Control and layout styles stay loaded.</summary>
public sealed class AppThemeManager : IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private readonly Application application;
    private readonly ResourceDictionary lightPalette;
    private readonly ResourceDictionary darkPalette;
    private ResourceDictionary? activePalette;
    private bool disposed;

    public AppThemePreference Preference { get; private set; } = AppThemePreference.System;
    public bool IsDark { get; private set; }

    public AppThemeManager(Application application)
    {
        this.application = application;
        lightPalette = LoadPalette("Light");
        darkPalette = LoadPalette("Dark");
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public void SetPreference(AppThemePreference preference)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Preference = preference;
        Apply(preference == AppThemePreference.Dark ||
              (preference == AppThemePreference.System && SystemPrefersDark()));
    }

    public void ApplyTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var dark = IsDark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, 20, ref dark, Marshal.SizeOf<int>());
    }

    private void Apply(bool dark)
    {
        var palette = dark ? darkPalette : lightPalette;
        if (!ReferenceEquals(activePalette, palette))
        {
            var dictionaries = application.Resources.MergedDictionaries;
            if (activePalette is not null)
            {
                dictionaries.Remove(activePalette);
            }

            dictionaries.Insert(0, palette);
            activePalette = palette;
        }

        IsDark = dark;
        foreach (Window window in application.Windows)
        {
            ApplyTitleBar(window);
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (Preference != AppThemePreference.System || application.Dispatcher.HasShutdownStarted)
        {
            return;
        }

        application.Dispatcher.BeginInvoke(() => Apply(SystemPrefersDark()));
    }

    private static bool SystemPrefersDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("AppsUseLightTheme") is int setting && setting == 0;
    }

    private static ResourceDictionary LoadPalette(string name) => new()
    {
        Source = new Uri($"Themes/Palette.{name}.xaml", UriKind.Relative)
    };

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        disposed = true;
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
