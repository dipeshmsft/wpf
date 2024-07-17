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
                if(AppResourceContainsFluentDictionary(out string theme))
                {
                    Application.Current._theme = theme;
                }
            }
        }
    }


    internal static void OnSystemThemeChanged()
    {
        if(IsFluentThemeEnabled)
        {
            bool useLightColors = GetUseLightColors(Application.Current.Theme);
            var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
            AddOrUpdateThemeResources(fluentThemeResourceUri);
            
            foreach(Window window in Application.Current.Windows)
            {
                if(window == null) continue;

                if(window.Theme == "None")
                {
                    ApplyStyleOnWindow(window, useLightColors);
                    continue;
                }

                bool windowUseLightColors = GetUseLightColors(window.Theme);
                var windowFluentThemeResourceUri = GetFluentThemeResourceUri(windowUseLightColors);
                AddOrUpdateThemeResources(window, windowFluentThemeResourceUri);
                ApplyStyleOnWindow(window, windowUseLightColors);
            }
        }
        else
        {
            foreach(Window window in _fluentEnabledWindows)
            {
                if(window == null) continue;

                if(window.Theme == "None")
                {
                    RemoveFluentFromWindow(window);
                    continue;
                }

                bool windowUseLightColors = GetUseLightColors(window.Theme);
                var windowFluentThemeResourceUri = GetFluentThemeResourceUri(windowUseLightColors);
                AddOrUpdateThemeResources(window, windowFluentThemeResourceUri);
                ApplyStyleOnWindow(window, windowUseLightColors);
            }
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

        // Hack to prevent resetting of the resources when the application properties are being parsed
        if(Application.Current.Windows.Count == 0)
        {
            _deferredThemeLoading = true;
        }

        bool useLightColors = GetUseLightColors(newTheme);
        var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
        AddOrUpdateThemeResources(fluentThemeResourceUri);

        foreach(Window window in Application.Current.Windows)
        {
            if(!_fluentEnabledWindows.HasItem(window))
            {
                ApplyStyleOnWindow(window, useLightColors);
            }
        }

        // _currentUseLightMode = useLightColors;
        // _currentFluentThemeResourceUri = fluentThemeResourceUri;
        // _currentAccentColor = AccentColorHelper.GetAccentColor();
    }

    internal static void OnWindowThemeChanged(Window window, string oldTheme, string newTheme)
    {
        if(window == null) return;

        if(newTheme == "None")
        {
            RemoveFluentFromWindow(window);

            if(_fluentEnabledWindows.HasItem(window))
            {
                _fluentEnabledWindows.Remove(window);
            }

            return;
        }

        bool useLightColors = GetUseLightColors(newTheme);
        var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
        AddOrUpdateThemeResources(window, fluentThemeResourceUri);
        ApplyStyleOnWindow(window, useLightColors);

        if(!_fluentEnabledWindows.HasItem(window))
        {
            _fluentEnabledWindows.Add(window);
        }
    }

    internal static void LoadDeferredApplicationTheme()
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
        if(!IsFluentThemeEnabled && window.Theme == "None") return false;

        bool useLightColors = GetUseLightColors(Application.Current.Theme);
        if(window.Theme != "None")
        {
            useLightColors = GetUseLightColors(window.Theme);
        }
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
        int index = FindFluentThemeResourceDictionary();
        if(index != -1)
        {
            Application.Current.Resources.MergedDictionaries.RemoveAt(index);
        }

        foreach(Window window in Application.Current.Windows)
        {
            if(!_fluentEnabledWindows.HasItem(window))
            {
                RemoveStyleOnWindow(window);
            }
        }
    }

    private static void RemoveFluentFromWindow(Window window)
    {
        if(window == null) return;

        int index = FindFluentThemeResourceDictionary(window);
        if(index != -1)
        {
            window.Resources.MergedDictionaries.RemoveAt(index);
        }

        RemoveStyleOnWindow(window);
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
        if(IsFluentThemeEnabled)
        {
            bool useLightColors = GetUseLightColors(Application.Current.Theme);
            SetImmersiveDarkMode(window, !useLightColors);
        }
        else
        {
            // TODO : Remove the styles from windows which have BackdropDisabledWidowStyle
            SetImmersiveDarkMode(window, false);
            WindowBackdropManager.SetBackdrop(window, WindowBackdropType.None);
        }
    }

    #endregion

    #region Helper Methods

    private static bool GetUseLightColors(string theme)
    {
        // Do we need to add a check for "None" theme?
        return theme == "Light" || theme == "System" && IsSystemThemeLight();
    }

    private static bool AppResourceContainsFluentDictionary(out string theme)
    {
        theme = "None";
        if(Application.Current != null)
        {
            string dictionarySource;
            ResourceDictionary rd = Application.Current.Resources;
            
            for(int i = rd.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                dictionarySource = rd.MergedDictionaries[i].Source.ToString();
                if(dictionarySource.Contains(fluentThemeResoruceDictionaryUri, StringComparison.OrdinalIgnoreCase))
                {
                    if(dictionarySource.Contains("Fluent.Light", StringComparison.OrdinalIgnoreCase))
                    {
                        theme = "Light";
                    }
                    else if(dictionarySource.Contains("Fluent.Dark", StringComparison.OrdinalIgnoreCase))
                    {
                        theme = "Dark";
                    }
                    else
                    {
                        theme = "System";
                    }
                    return true;
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

        int index = FindFluentThemeResourceDictionary();
        
        if (index != -1)
        {
            Application.Current.Resources.MergedDictionaries.RemoveAt(index);
            Application.Current.Resources.MergedDictionaries.Insert(index, newDictionary);
        }
        else
        {
            Application.Current.Resources.MergedDictionaries.Add(newDictionary);
        }

    }

    private static void AddOrUpdateThemeResources(Window window, Uri dictionaryUri)
    {
        ArgumentNullException.ThrowIfNull(dictionaryUri, nameof(dictionaryUri));

        var newDictionary = new ResourceDictionary() { Source = dictionaryUri };

        int index = FindFluentThemeResourceDictionary(window);
        
        if (index != -1)
        {
            window.Resources.MergedDictionaries.RemoveAt(index);
            window.Resources.MergedDictionaries.Insert(index, newDictionary);
        }
        else
        {
            window.Resources.MergedDictionaries.Add(newDictionary);
        }
    }

    private static int FindFluentThemeResourceDictionary()
    {
        if (Application.Current == null) return -1;
        return FindLastFlunetThemeResourceDictionaryIndex(Application.Current.Resources);
    }

    private static int FindFluentThemeResourceDictionary(Window window)
    {
        if(window == null) return -1;
        return FindLastFlunetThemeResourceDictionaryIndex(window.Resources);
    }

    private static int FindLastFlunetThemeResourceDictionaryIndex(ResourceDictionary rd)
    {
        if(rd == null) return -1;

        for(int i = rd.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if(rd.MergedDictionaries[i].Source != null)
            {
                if(rd.MergedDictionaries[i].Source.ToString().Contains(fluentThemeResoruceDictionaryUri))
                {
                    return i;
                }
            }
        }
        return -1;
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
    internal static WindowCollection _fluentEnabledWindows = new WindowCollection();

    // private static bool _currentUseLightMode;
    // private static bool _currentFluentThemeResourceUri;
    // private static Color _currentAccentColor;
    
    #endregion
}