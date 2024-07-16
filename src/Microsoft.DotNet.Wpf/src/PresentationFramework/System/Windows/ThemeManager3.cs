using Standard;
using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Appearance;

namespace System.Windows;

public static class ThemeManager3
{
    static ThemeManager3()
    {
        if(Application.Current != null)
        {
            if(Application.Current.Theme == "None")
            {
                if(AppResourceContainsFluentDictionary())
                {
                    Application.Current._theme = "System";
                }
            }
        }
    }


    internal static void OnSystemThemeChanged()
    {
        if(!IsFluentThemeEnabled) return;

        bool useLightColors = GetUseLightColors(Application.Current.Theme);
        var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
        AddOrUpdateThemeResources(fluentThemeResourceUri);
        foreach(Window window in Application.Current.Windows)
        {
            ApplyStyleOnWindow(window, useLightColors);
        }
        
        // Color accentColor = AccentColorHelper.GetAccentColor();

        // if(useLightColors != _currentUseLightMode 
        //         || fluentThemeResourceUri != _currentFluentThemeResourceUri 
        //         || accentColor != _currentAccentColor)
        // {
        //     AddOrUpdateThemeResources(fluentThemeResourceUri);

        //     foreach(Window window in Application.Current.Windows)
        //     {
        //         ApplyStyleOnWindow(window, useLightColors);
        //     }

        //     _currentUseLightMode = useLightColors;
        //     _currentFluentThemeResourceUri = fluentThemeResourceUri;
        //     _currentAccentColor = accentColor;
        // }
    }

    internal static void OnApplicationThemeChanged(string oldTheme, string newTheme)
    {
        if(!IsFluentThemeEnabled) return;

        if(newTheme == "None")
        {
            RemoveFluentFromApplication();
            return;
        }

        if(Application.Current.Windows.Count == 0)
        {
            _deferredThemeLoading = true;
        }

        bool useLightColors = GetUseLightColors(newTheme);
        var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
        AddOrUpdateThemeResources(fluentThemeResourceUri);

        foreach(Window window in Application.Current.Windows)
        {
            ApplyStyleOnWindow(window, useLightColors);
        }

        // _currentUseLightMode = useLightColors;
        // _currentFluentThemeResourceUri = fluentThemeResourceUri;
        // _currentAccentColor = AccentColorHelper.GetAccentColor();
    }

    internal static void LoadDeferrredTheme()
    {
        if(_deferredThemeLoading)
        {
            _deferredThemeLoading = false;
            OnApplicationThemeChanged("None", Application.Current.Theme);
        }
    }

    internal static bool VerifyApplicationTheme(string theme)
    {
        return theme == "System" || theme == "Light" || theme == "Dark" || theme == "None";
    }

    internal static bool ApplyStyleOnWindow(Window window)
    {
        if(!IsFluentThemeEnabled) return false;

        bool useLightColors = GetUseLightColors(Application.Current.Theme);
        ApplyStyleOnWindow(window, useLightColors);
        return true;
    }

    #region Internal Properties
    internal static bool IsFluentThemeEnabled
    {
        get
        {
            if(Application.Current == null) return false;
            return Application.Current.Theme != "None";
        }
    }

    internal static double DefaultFluentThemeFontSize => 14;
    
    #endregion

    #region Private Methods

    private static void RemoveFluentFromApplication()
    {
        FindFluentThemeResourceDictionary(out ResourceDictionary fluentDictionary);
        if(fluentDictionary != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(fluentDictionary);
        }

        foreach(Window window in Application.Current.Windows)
        {
            RemoveStyleOnWindow(window);
        }
    }

    private static void ApplyStyleOnWindow(Window window, bool useLightColors)
    {
        if(SystemParameters.HighContrast)
        {
            window.SetResourceReference(FrameworkElement.StyleProperty, "BackdropDisabledWindowStyle");
            WindowBackdropManager.SetBackdrop(window, WindowBackdropType.None);
        }
        else
        {
            window.SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
            SetImmersiveDarkMode(window, !useLightColors);
            WindowBackdropManager.SetBackdrop(window, WindowBackdropType.MainWindow);
        }
    }

    private static void RemoveStyleOnWindow(Window window)
    {
        // TODO : Remove the styles from windows which have BackdropDisabledWidowStyle
        SetImmersiveDarkMode(window, false);
        WindowBackdropManager.SetBackdrop(window, WindowBackdropType.None);
    }

    #endregion

    #region Helper Methods

    private static bool GetUseLightColors(string theme)
    {
        // Do we need to add a check for "None" theme?
        return theme == "Light" || theme == "System" && IsSystemThemeLight();
    }

    private static bool AppResourceContainsFluentDictionary()
    {
        if(Application.Current != null)
        {
            string dictionarySource;
            foreach(ResourceDictionary mergedDictionary in Application.Current.Resources.MergedDictionaries)
            {
                if(mergedDictionary.Source != null)
                {
                    dictionarySource = mergedDictionary.Source.ToString();

                    if(dictionarySource.Contains(fluentThemeResoruceDictionaryUri, 
                                                    StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    
    private static Uri GetFluentThemeResourceUri(bool useLightMode)
    {
        string themeFileName = "Fluent." + (useLightMode ? "Light" : "Dark") + ".xaml";

        if(SystemParameters.HighContrast)
        {
            themeFileName = "Fluent.HC.xaml";
        }

        return new Uri(fluentThemeResoruceDictionaryUri + themeFileName, UriKind.Absolute);
    }  

    private static void AddOrUpdateThemeResources(Uri dictionaryUri)
    {
        ArgumentNullException.ThrowIfNull(dictionaryUri, nameof(dictionaryUri));

        var newDictionary = new ResourceDictionary() { Source = dictionaryUri };

        FindFluentThemeResourceDictionary(out ResourceDictionary fluentDictionary);
        
        if (fluentDictionary != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(fluentDictionary);
        }

        Application.Current.Resources.MergedDictionaries.Add(newDictionary);
    }

    private static void FindFluentThemeResourceDictionary(out ResourceDictionary fluentDictionary)
    {
        fluentDictionary = null;

        if (Application.Current == null) return;

        foreach (ResourceDictionary mergedDictionary in Application.Current.Resources.MergedDictionaries)
        {
            if (mergedDictionary.Source != null)
            {
                if (mergedDictionary.Source.ToString().Contains(fluentThemeResoruceDictionaryUri))
                {
                    fluentDictionary = mergedDictionary;
                    break;
                }
            }
        }
    }

    private static bool IsSystemThemeLight()
    {
        return ThemeManager.IsSystemThemeLight();
    }    

    private static bool SetImmersiveDarkMode(Window window, bool useDarkMode)
    {
        return ThemeManager.SetImmersiveDarkMode(window, useDarkMode);
    }

    #endregion

    #region Private Fields
    private static readonly string fluentThemeResoruceDictionaryUri = "pack://application:,,,/PresentationFramework.Fluent;component/Themes/";
    private static bool _deferredThemeLoading = false;
    // private static bool _currentUseLightMode;
    // private static bool _currentFluentThemeResourceUri;
    // private static Color _currentAccentColor;
    
    #endregion
}