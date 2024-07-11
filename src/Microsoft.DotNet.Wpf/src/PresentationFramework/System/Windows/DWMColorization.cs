using System;
using System.Diagnostics;
using System.Windows.Appearance;
using System.Windows.Media;
using Microsoft.Win32;
using MS.Internal;
using System.Runtime.InteropServices;
using MS.Internal.WindowsRuntime.Windows.UI.ViewManagement;

namespace System.Windows;
internal static class DwmColorization
{
    private static Color _currentApplicationAccentColor = Color.FromArgb(0xff, 0x00, 0x78, 0xd4);

    private static UISettings _uiSettings = null;

    /// <summary>
    ///     Gets the current application accent color.
    /// </summary>
    internal static Color CurrentApplicationAccentColor
    {
        get { return _currentApplicationAccentColor; }
    }

    internal static UISettings _UISettings
    {
        get
        {
            if (_uiSettings == null)
            {
                _uiSettings = new UISettings();
            }

            return _uiSettings;
        }
    }

    /// <summary>
    /// Gets the system accent color.
    /// </summary>
    /// <returns>Updated <see cref="System.Windows.Media.Color"/> Accent Color.</returns>
    internal static Color GetSystemAccentColor()
    {
        _UISettings.TryGetColorValue(UISettingsRCW.UIColorType.Accent, out Color systemAccent);
        return systemAccent;
    }

    /// <summary>
    /// Computes the current Accent Colors and calls for updating of accent color values in resource dictionary
    /// </summary>
    internal static void UpdateAccentColors()
    {
        Color systemAccent = GetSystemAccentColor();
        Color primaryAccent, secondaryAccent, tertiaryAccent;

        if (systemAccent != _currentApplicationAccentColor)
        {
            _UISettings.TryUpdateAccentColors();
        }

        bool useLightModeColors = ThemeManager.IsSystemThemeLight();
        GetAccentColorVariations(useLightModeColors, out primaryAccent, out secondaryAccent, out tertiaryAccent);
        UpdateColorResources(useLightModeColors, systemAccent, primaryAccent, secondaryAccent, tertiaryAccent);
        _currentApplicationAccentColor = systemAccent;
    }

    internal static void UpdateAccentColors(string requestedTheme)
    {
        Color systemAccent = GetSystemAccentColor();
        bool useLightModeColors = requestedTheme == "Light";
        GetAccentColorVariations(useLightModeColors, out Color primaryAccent, out Color secondaryAccent, out Color tertiaryAccent);
        UpdateColorResources(useLightModeColors, systemAccent, primaryAccent, secondaryAccent, tertiaryAccent);
        _currentApplicationAccentColor = systemAccent;
    }

    internal static void UpdateAccentColors(Window window, string requestedTheme)
    {
        Color systemAccent = GetSystemAccentColor();
        bool useLightModeColors = requestedTheme == "Light";
        GetAccentColorVariations(useLightModeColors, out Color primaryAccent, out Color secondaryAccent, out Color tertiaryAccent);
        UpdateColorResourcesOnWindow(window, useLightModeColors, systemAccent, primaryAccent, secondaryAccent, tertiaryAccent);
    }
    

    private static void GetAccentColorVariations(bool lightModeColors, out Color primaryAccent, out Color secondaryAccent, out Color tertiaryAccent)
    {
        if (lightModeColors)
        {
            // In light mode, we use darker shades of the accent color
            primaryAccent = _UISettings.AccentDark1;
            secondaryAccent = _UISettings.AccentDark2;
            tertiaryAccent = _UISettings.AccentDark3;
        }
        else
        {
            // In dark mode, we use lighter shades of the accent color
            primaryAccent = _UISettings.AccentLight1;
            secondaryAccent = _UISettings.AccentLight2;
            tertiaryAccent = _UISettings.AccentLight3;
        }
    }

    /// <summary>
    /// Updates application resources.
    /// </summary>        
    private static void UpdateColorResources(
        bool useLightModeColors,
        Color systemAccent,
        Color primaryAccent,
        Color secondaryAccent,
        Color tertiaryAccent)
    {

        if (useLightModeColors)
        {
            Application.Current.Resources["TextOnAccentFillColorPrimary"] = 
                    Color.FromArgb( 0xFF, 0x00, 0x00, 0x00);
            
            Application.Current.Resources["TextOnAccentFillColorSecondary"] = 
                    Color.FromArgb( 0x80, 0x00, 0x00, 0x00);
            
            Application.Current.Resources["TextOnAccentFillColorDisabled"] = 
                    Color.FromArgb( 0x77, 0x00, 0x00, 0x00);
            
            Application.Current.Resources["TextOnAccentFillColorSelectedText"] = 
                    Color.FromArgb( 0x00, 0x00, 0x00, 0x00);
            
            Application.Current.Resources["AccentTextFillColorDisabled"] = 
                    Color.FromArgb( 0x5D, 0x00, 0x00, 0x00);
        }
        else
        {

            Application.Current.Resources["TextOnAccentFillColorPrimary"] = 
                    Color.FromArgb( 0xFF, 0xFF, 0xFF, 0xFF);
            
            Application.Current.Resources["TextOnAccentFillColorSecondary"] = 
                    Color.FromArgb( 0x80, 0xFF, 0xFF, 0xFF);
            
            Application.Current.Resources["TextOnAccentFillColorDisabled"] = 
                    Color.FromArgb( 0x87, 0xFF, 0xFF, 0xFF);
            
            Application.Current.Resources["TextOnAccentFillColorSelectedText"] = 
                    Color.FromArgb( 0xFF, 0xFF, 0xFF, 0xFF);
            
            Application.Current.Resources["AccentTextFillColorDisabled"] = 
                    Color.FromArgb( 0x5D, 0xFF, 0xFF, 0xFF);
        }

        Application.Current.Resources["SystemAccentColor"] = systemAccent;
        Application.Current.Resources["SystemAccentColorPrimary"] = primaryAccent;
        Application.Current.Resources["SystemAccentColorSecondary"] = secondaryAccent;
        Application.Current.Resources["SystemAccentColorTertiary"] = tertiaryAccent;

        Application.Current.Resources["SystemAccentBrush"] = ToBrush(systemAccent);
        Application.Current.Resources["SystemFillColorAttentionBrush"] = ToBrush(secondaryAccent);
        Application.Current.Resources["AccentTextFillColorPrimaryBrush"] = ToBrush(tertiaryAccent);
        Application.Current.Resources["AccentTextFillColorSecondaryBrush"] = ToBrush(tertiaryAccent);
        Application.Current.Resources["AccentTextFillColorTertiaryBrush"] = ToBrush(secondaryAccent);
        Application.Current.Resources["AccentFillColorSelectedTextBackgroundBrush"] = ToBrush(systemAccent);
        Application.Current.Resources["AccentFillColorDefaultBrush"] = ToBrush(secondaryAccent);

        Application.Current.Resources["AccentFillColorSecondaryBrush"] = ToBrush(secondaryAccent, 0.9);
        Application.Current.Resources["AccentFillColorTertiaryBrush"] = ToBrush(secondaryAccent, 0.8);
    }


    private static void UpdateColorResourcesOnWindow(
        Window window,
        bool useLightModeColors,
        Color systemAccent,
        Color primaryAccent,
        Color secondaryAccent,
        Color tertiaryAccent)
    {
        ResourceDictionary resources = window.Resources;

        UpdateTextOnAccentColorResourcesInternal(resources, useLightModeColors);
        UpdateAccentColorResourcesInternal(resources, systemAccent, primaryAccent, secondaryAccent, tertiaryAccent);
    }

    private static void UpdateTextOnAccentColorResourcesInternal(ResourceDictionary rd, bool useLightModeColors)
    {
        if (useLightModeColors)
        {
            rd["TextOnAccentFillColorPrimary"] = 
                    Color.FromArgb( 0xFF, 0x00, 0x00, 0x00);
            
            rd["TextOnAccentFillColorSecondary"] = 
                    Color.FromArgb( 0x80, 0x00, 0x00, 0x00);
            
            rd["TextOnAccentFillColorDisabled"] = 
                    Color.FromArgb( 0x77, 0x00, 0x00, 0x00);
            
            rd["TextOnAccentFillColorSelectedText"] = 
                    Color.FromArgb( 0x00, 0x00, 0x00, 0x00);
            
            rd["AccentTextFillColorDisabled"] = 
                    Color.FromArgb( 0x5D, 0x00, 0x00, 0x00);
        }
        else
        {

            rd["TextOnAccentFillColorPrimary"] = 
                    Color.FromArgb( 0xFF, 0xFF, 0xFF, 0xFF);
            
            rd["TextOnAccentFillColorSecondary"] = 
                    Color.FromArgb( 0x80, 0xFF, 0xFF, 0xFF);
            
            rd["TextOnAccentFillColorDisabled"] = 
                    Color.FromArgb( 0x87, 0xFF, 0xFF, 0xFF);
            
            rd["TextOnAccentFillColorSelectedText"] = 
                    Color.FromArgb( 0xFF, 0xFF, 0xFF, 0xFF);
            
            rd["AccentTextFillColorDisabled"] = 
                    Color.FromArgb( 0x5D, 0xFF, 0xFF, 0xFF);
        }

    }

    private static void UpdateAccentColorResourcesInternal(ResourceDictionary rd, Color systemAccent, Color primaryAccent, Color secondaryAccent, Color tertiaryAccent)
    {
        rd["SystemAccentColor"] = systemAccent;
        rd["SystemAccentColorPrimary"] = primaryAccent;
        rd["SystemAccentColorSecondary"] = secondaryAccent;
        rd["SystemAccentColorTertiary"] = tertiaryAccent;

        rd["SystemAccentBrush"] = ToBrush(systemAccent);
        rd["SystemFillColorAttentionBrush"] = ToBrush(secondaryAccent);
        rd["AccentTextFillColorPrimaryBrush"] = ToBrush(tertiaryAccent);
        rd["AccentTextFillColorSecondaryBrush"] = ToBrush(tertiaryAccent);
        rd["AccentTextFillColorTertiaryBrush"] = ToBrush(secondaryAccent);
        rd["AccentFillColorSelectedTextBackgroundBrush"] = ToBrush(systemAccent);
        rd["AccentFillColorDefaultBrush"] = ToBrush(secondaryAccent);

        rd["AccentFillColorSecondaryBrush"] = ToBrush(secondaryAccent, 0.9);
        rd["AccentFillColorTertiaryBrush"] = ToBrush(secondaryAccent, 0.8);

    }

    /// <summary>
    /// Creates a <see cref="SolidColorBrush"/> from a <see cref="System.Windows.Media.Color"/> with defined brush opacity.
    /// </summary>
    /// <param name="color">Input color.</param>
    /// <param name="opacity">Degree of opacity.</param>
    /// <returns>Brush converted to color with modified opacity.</returns>
    private static SolidColorBrush ToBrush(Color color, double opacity = 1.0)
    {
        return new SolidColorBrush { Color = color, Opacity = opacity };
    }
}