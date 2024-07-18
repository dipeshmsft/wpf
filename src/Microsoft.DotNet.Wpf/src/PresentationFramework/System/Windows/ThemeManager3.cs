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
    internal static void OnSystemThemeChanged()
    {
        if(IsFluentThemeEnabled)
        {
            bool useLightColors = GetUseLightColors(Application.Current.Theme);
            var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
            AddOrUpdateThemeResources(Application.Current.Resources, fluentThemeResourceUri);
            
            foreach(Window window in Application.Current.Windows)
            {
                if(window == null) continue;

                if(window.Theme == "None")
                {
                    ApplyStyleOnWindow(window, useLightColors);
                    continue;
                }

                ApplyFluentOnWindow(window);
            }
        }
        else
        {
            foreach(Window window in _fluentEnabledWindows)
            {
                if(window == null || window.IsDisposed) continue;

                // This seems redundant. Because as soon as we set window.Theme to None, it will be removed from the list.
                if(window.Theme == "None")
                {
                    RemoveFluentFromWindow(window);
                    continue;
                }

                ApplyFluentOnWindow(window);
            }
        }        
    }

    internal static void OnApplicationThemeChanged(string oldTheme, string newTheme)
    {
        IgnoreAppResourceDictionaryChanges = true;

        try
        {        
            if(newTheme == "None")
            {
                RemoveFluentFromApplication();
                return;
            }

            // Hack to prevent resetting of the resources 
            // when the application properties are being parsed
            if(Application.Current.Windows.Count == 0)
            {
                _deferThemeLoading = true;
            }

            bool useLightColors = GetUseLightColors(newTheme);
            var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
            AddOrUpdateThemeResources(Application.Current.Resources, fluentThemeResourceUri);

            foreach(Window window in Application.Current.Windows)
            {
                if(!_fluentEnabledWindows.HasItem(window))
                {
                    ApplyStyleOnWindow(window, useLightColors);
                }
            }

        }
        finally
        {
            IgnoreAppResourceDictionaryChanges = false;
        }
    }

    internal static void OnAppResourcesChanged()
    {
        if(IgnoreAppResourceDictionaryChanges) return;

        if(AppResourceContainsFluentDictionary(out string theme))
        {
            Application.Current.Theme = theme;
        }
        else
        {
            Application.Current.Theme = "None";
        }

    }

    internal static void OnWindowThemeChanged(Window window, string oldTheme, string newTheme)
    {
        // Redundant
        if(window == null) return;

        if(newTheme == "None")
        {
            RemoveFluentFromWindow(window);
            return;
        }

        ApplyFluentOnWindow(window);
    }

    internal static void LoadDeferredApplicationTheme()
    {
        if(_deferThemeLoading)
        {
            _deferThemeLoading = false;
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
    internal static bool IgnoreAppResourceDictionaryChanges
    {
        get
        {
            return _ignoreAppResourceDictionaryChanges;
        }
        set
        {
            _ignoreAppResourceDictionaryChanges = value;
        }
    }

    internal static double DefaultFluentThemeFontSize => 14;
    
    #endregion

    #region Private Methods

    private static void RemoveFluentFromApplication()
    {
        if(Application.Current == null) return;

        int index = FindLastFluentThemeResourceDictionaryIndex(Application.Current.Resources);
        
        if(index != -1)
        {
            Application.Current.Resources.MergedDictionaries.RemoveAt(index);
        }

        foreach(Window window in Application.Current.Windows)
        {
            if(!_fluentEnabledWindows.HasItem(window))
            {
                RemoveStyleFromWindow(window);
            }
        }
    }

    private static void RemoveFluentFromWindow(Window window)
    {
        if(window == null) return;

        int index = FindLastFluentThemeResourceDictionaryIndex(window.Resources);
        
        if(index != -1)
        {
            window.Resources.MergedDictionaries.RemoveAt(index);
        }

        RemoveStyleFromWindow(window);

        _fluentEnabledWindows.Remove(window);
    }

    private static void ApplyFluentOnWindow(Window window)
    {
        if(window == null) return;

        bool useLightColors = GetUseLightColors(window.Theme);
        var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
        AddOrUpdateThemeResources(window.Resources, fluentThemeResourceUri);
        ApplyStyleOnWindow(window, useLightColors);

        if(!_fluentEnabledWindows.HasItem(window))
        {
            _fluentEnabledWindows.Add(window);
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
            window.SetImmersiveDarkMode(!useLightColors);
            WindowBackdropManager.SetBackdrop(window, WindowBackdropType.MainWindow);
        }
    }

    private static void RemoveStyleFromWindow(Window window)
    {
        if(window == null || window.IsDisposed) return;
        if(IsFluentThemeEnabled)
        {
            bool useLightColors = GetUseLightColors(Application.Current.Theme);
            window.SetImmersiveDarkMode(!useLightColors);
        }
        else
        {
            // TODO : Remove the styles from windows which have BackdropDisabledWidowStyle
            window.SetImmersiveDarkMode(false);
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
            ResourceDictionary rd = Application.Current.Resources;
            int index = FindLastFluentThemeResourceDictionaryIndex(rd);

            if(index != -1)
            {
                string dictionarySource = rd.MergedDictionaries[index].Source.ToString();
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

    private static void AddOrUpdateThemeResources(ResourceDictionary rd, Uri dictionaryUri)
    {
        if(rd == null) return;

        ArgumentNullException.ThrowIfNull(dictionaryUri, nameof(dictionaryUri));

        var newDictionary = new ResourceDictionary() { Source = dictionaryUri };

        int index = FindLastFluentThemeResourceDictionaryIndex(rd);
        
        if (index != -1)
        {
            rd.MergedDictionaries[index] = newDictionary;
        }
        else
        {
            rd.MergedDictionaries.Insert(0, newDictionary);
        }
    }

    private static int FindLastFluentThemeResourceDictionaryIndex(ResourceDictionary rd)
    {
        if(rd == null) return -1;

        for(int i = rd.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if(rd.MergedDictionaries[i].Source != null)
            {
                if(rd.MergedDictionaries[i].Source.ToString().StartsWith(fluentThemeResoruceDictionaryUri))
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private static bool IsSystemThemeLight()
    {
        var useLightTheme = Registry.GetValue(_regPersonalizeKeyPath,
            "AppsUseLightTheme", null) as int?;

        if (useLightTheme == null)
        {
            useLightTheme = Registry.GetValue(_regPersonalizeKeyPath,
                "SystemUsesLightTheme", null) as int?;
        }

        return useLightTheme != null && useLightTheme != 0;
    }    

    #endregion

    #region Private Fields
    private static bool _deferThemeLoading = false;
    private static bool _ignoreAppResourceDictionaryChanges = false;
    internal static WindowCollection _fluentEnabledWindows = new WindowCollection();
    private static readonly string fluentThemeResoruceDictionaryUri = "pack://application:,,,/PresentationFramework.Fluent;component/Themes/";
    private static readonly string _regPersonalizeKeyPath = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

    
    // private static FluentThemeState _fluentThemeState = new FluentThemeState();

    #endregion
}