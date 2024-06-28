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

    static DwmColorization()
    {
        UpdateCachedAccentColors();
    }


    #region Accent Colors Properties

    internal static Color AccentColor
    {
        get { return _currentApplicationAccentColor; }
    }

    internal static Color AccentColorLight1 { get; set; }
    internal static Color AccentColorLight2 { get; set; }
    internal static Color AccentColorLight3 { get; set; }
    internal static Color AccentColorDark1 { get; set; }
    internal static Color AccentColorDark2 { get; set; }
    internal static Color AccentColorDark3 { get; set; }

    #endregion

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

        if (systemAccent != _currentApplicationAccentColor)
        {
            _UISettings.TryUpdateAccentColors();
        }

        if (ThemeManager.IsSystemThemeLight())
        {
            // In light mode, we use darker shades of the accent color
            AccentColorDark1 = _UISettings.AccentDark1;
            AccentColorDark2 = _UISettings.AccentDark2;
            AccentColorDark3 = _UISettings.AccentDark3;
        }
        else
        {
            // In dark mode, we use lighter shades of the accent color
            AccentColorLight1 = _UISettings.AccentLight1;
            AccentColorLight2 = _UISettings.AccentLight2;
            AccentColorLight3 = _UISettings.AccentLight3;
        }

        UpdateColorResources(systemAccent, AccentColorLight1, AccentColorLight2, AccentColorLight3);
        _currentApplicationAccentColor = systemAccent;
    }

    internal static void UpdateCachedAccentColors()
    {
        Color systemAccent = GetSystemAccentColor();

        if (systemAccent != _currentApplicationAccentColor)
        {
            _UISettings.TryUpdateAccentColors();
        }

        if (ThemeManager.IsSystemThemeLight())
        {
            // In light mode, we use darker shades of the accent color
            AccentColorDark1 = _UISettings.AccentDark1;
            AccentColorDark2 = _UISettings.AccentDark2;
            AccentColorDark3 = _UISettings.AccentDark3;
        }
        else
        {
            // In dark mode, we use lighter shades of the accent color
            AccentColorLight1 = _UISettings.AccentLight1;
            AccentColorLight2 = _UISettings.AccentLight2;
            AccentColorLight3 = _UISettings.AccentLight3;
        }

        _currentApplicationAccentColor = systemAccent;
    }

    /// <summary>
    /// Updates application resources.
    /// </summary>        
    private static void UpdateColorResources(
        Color systemAccent,
        Color primaryAccent,
        Color secondaryAccent,
        Color tertiaryAccent)
    {

        if (!ThemeManager.IsSystemThemeLight())
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