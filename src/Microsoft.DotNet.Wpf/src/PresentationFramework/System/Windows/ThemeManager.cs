using Standard;
using System.Windows.Appearance;
using System.Windows.Media;
using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Interop;

namespace System.Windows;

internal static partial class ThemeManager
{

    #region Constructor

    static ThemeManager()
    {
        _fluentEnabledWindows = new List<Window>();

        bool resourcesFound = FindFluentThemeAndColorResourceInApp(out ResourceDictionary themeDictionary, out ResourceDictionary colorDictionary);

        if(themeDictionary == null) return;

        if(!resourcesFound)
        {
            Application.Current.Theme = "Fluent";
        }
        else
        {
            Application.Current.Theme = GetApplicationThemeFromColorDictionary(colorDictionary);
        }

        _isFluentThemeEnabled = true;
        _isFluentThemeInitialized = true;
    }

    #endregion

    #region Internal Methods

    internal static void InitializeFluentTheme()
    {
        if(IsFluentThemeEnabled && !_isFluentThemeInitialized)
        {
            _currentApplicationTheme = GetSystemTheme();
            _currentUseLightMode = IsSystemThemeLight();

            var themeColorResourceUri = GetFluentWindowThemeColorResourceUri(_currentApplicationTheme, _currentUseLightMode);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = themeColorResourceUri });

            DwmColorization.UpdateAccentColors();
            _isFluentThemeInitialized = true;
        }

        _fluentEnabledWindows = new List<Window>();
    }

    /// <summary>
    ///    Apply the system theme one window.
    /// </summary>
    /// <param name="forceUpdate"></param>
    internal static void ApplySystemTheme(Window window, bool forceUpdate = false)
    {
        ApplySystemTheme(new List<Window> { window }, forceUpdate);
    }

    /// <summary>
    ///   Apply the system theme to a list of windows.
    ///   If windows is not provided, apply the theme to all windows in the application.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="forceUpdate"></param>
    internal static void ApplySystemTheme(IEnumerable windows = null, bool forceUpdate = false)
    {
        if(windows == null)
        {
            // If windows is not provided, apply the theme to all windows in the application.
            windows = Application.Current?.Windows;
            
            if(windows == null)
            {
                return;
            }
        }

        string systemTheme = GetSystemTheme();
        bool useLightMode = IsSystemThemeLight();
        Color systemAccentColor = DwmColorization.GetSystemAccentColor();
        ApplyTheme(windows , systemTheme, useLightMode, systemAccentColor, forceUpdate);
    }

    /// <summary>
    ///  Apply the requested theme and color mode to the windows.
    ///  Checks if any update is needed before applying the changes.
    /// </summary>
    /// <param name="windows"></param>
    /// <param name="requestedTheme"></param>
    /// <param name="requestedUseLightMode"></param>
    /// <param name="requestedAccentColor"></param>
    /// <param name="forceUpdate"></param>
    private static void ApplyTheme(
        IEnumerable windows, 
        string requestedTheme, 
        bool requestedUseLightMode,
        Color requestedAccentColor, 
        bool forceUpdate = false)
    {
        if(forceUpdate || 
                requestedTheme != _currentApplicationTheme || 
                requestedUseLightMode != _currentUseLightMode ||
                DwmColorization.GetSystemAccentColor() != DwmColorization.CurrentApplicationAccentColor)
        {
            DwmColorization.UpdateAccentColors();

            Uri dictionaryUri = GetFluentWindowThemeColorResourceUri(requestedTheme, requestedUseLightMode);
            AddOrUpdateThemeResources(dictionaryUri);

            foreach(Window window in windows)
            {
                if(window == null)
                {
                    continue;
                }
                
                SetImmersiveDarkMode(window, !requestedUseLightMode);
                WindowBackdropManager.SetBackdrop(window, SystemParameters.HighContrast ? WindowBackdropType.None : WindowBackdropType.MainWindow);
            }

            _currentApplicationTheme = requestedTheme;
            _currentUseLightMode = requestedUseLightMode;
        }
    }

    /// <summary>
    ///  Set the immersive dark mode windowattribute for the window.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="useDarkMode"></param>
    /// <returns></returns>
    private static bool SetImmersiveDarkMode(Window window, bool useDarkMode)
    {
        if (window == null)
        {
            return false;
        }

        IntPtr handle = new WindowInteropHelper(window).Handle;

        if (handle != IntPtr.Zero)
        {
            var dwmResult = NativeMethods.DwmSetWindowAttributeUseImmersiveDarkMode(handle, useDarkMode);
            return dwmResult == HRESULT.S_OK;
        }

        return false;
    }

    #region Helper Methods

    /// <summary>
    ///   Reads the CurrentTheme registry key to fetch the system theme.
    ///   This along with UseLightTheme is used to determine the theme and color mode.
    /// </summary>
    /// <returns></returns>
    internal static string GetSystemTheme()
    {
        string systemTheme = Registry.GetValue(_regThemeKeyPath,
            "CurrentTheme", null) as string ?? "aero.theme";

        return systemTheme;
    }
   
    /// <summary>
    ///   Reads the AppsUseLightTheme registry key to fetch the color mode.
    ///   If the key is not present, it reads the SystemUsesLightTheme key.
    /// </summary>
    /// <returns></returns>
    internal static bool IsSystemThemeLight()
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

    /// <summary>
    ///  Update the Fluent theme resources with the values in new dictionary.
    /// </summary>
    /// <param name="dictionaryUri"></param>
    private static void AddOrUpdateThemeResources(Uri dictionaryUri)
    {
        ArgumentNullException.ThrowIfNull(dictionaryUri, nameof(dictionaryUri));

        var newDictionary = new ResourceDictionary() { Source = dictionaryUri };

        ResourceDictionary currentDictionary = Application.Current?.Resources;
        foreach (var key in newDictionary.Keys)
        {
            if (currentDictionary.Contains(key))
            {
                currentDictionary[key] = newDictionary[key];
            }
            else
            {
                currentDictionary.Add(key, newDictionary[key]);
            }
        }
    }

    #endregion

    #endregion

    #region Internal Properties

    internal static bool IsFluentThemeEnabled => _isFluentThemeEnabled;
    // TODO : Find a better way to deal with different default font sizes for different themes.
    internal static double DefaultFluentThemeFontSize => 14;

    #endregion

    #region Private Methods

    private static Uri GetFluentWindowThemeColorResourceUri(string systemTheme, bool useLightMode)
    {
        string themeColorFileName = useLightMode ? "light.xaml" : "dark.xaml";

        if(SystemParameters.HighContrast)
        {
            themeColorFileName = "hc.xaml";
        }

        return new Uri("pack://application:,,,/PresentationFramework.Fluent;component/Resources/Theme/" + themeColorFileName, UriKind.Absolute);
    }

    #endregion

    #region Private Members

    private static readonly string _regThemeKeyPath = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes";

    private static readonly string _regPersonalizeKeyPath = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

    private static string _currentApplicationTheme;

    private static bool _currentUseLightMode = true;

    private static bool _isFluentThemeEnabled = false;

    private static bool _isFluentThemeInitialized = false;

    #endregion
}


internal static partial class ThemeManager
{
    internal static void OnSystemThemeChanged()
    {
        if(SystemParameters.HighContrast)
        {
            ApplyFluentThemeOnApplication("HC", true);
            foreach(Window window in _fluentEnabledWindows)
            {
                ApplyFluentThemeOnWindow(window, "HC", true);
            }
        }
        else
        {
            if(Application.Current != null && Application.Current.Theme == "Fluent")
            {
                ApplyFluentThemeOnApplication("System", true);
            }
            // OnApplicationThemeChanged(null, Application.Current.Theme);
            foreach(Window window in _fluentEnabledWindows)
            {
                // OnWindowThemeChanged(window, null, window.Theme);
                if(window.Theme == "Fluent")
                {
                    ApplyFluentThemeOnWindow(window, "System", false);
                }
            }
        }
    }

    internal static void OnApplicationThemeChanged(string oldTheme, string newTheme)
    {
        switch (newTheme)
        {
            case "Fluent.Light":
                ApplyFluentThemeOnApplication("Light", true);
                break;
            case "Fluent.Dark":
                ApplyFluentThemeOnApplication("Dark", true);
                break;
            case "Fluent.HC":
                ApplyFluentThemeOnApplication("HC", true);
                break;
            case "Fluent":
                ApplyFluentThemeOnApplication("System", true);
                break;
            default:
                RemoveFluentThemeOnApplication();
                break;
        }
    }

    internal static void OnWindowThemeChanged(Window window, string oldTheme, string newTheme)
    {
        switch (newTheme)
        {
            case "Fluent.Light":
                ApplyFluentThemeOnWindow(window, "Light", true);
                break;
            case "Fluent.Dark":
                ApplyFluentThemeOnWindow(window, "Dark", true);
                break;
            case "Fluent.HC":
                ApplyFluentThemeOnWindow(window, "HC", true);
                break;
            case "Fluent":
                ApplyFluentThemeOnWindow(window, "System", true);
                break;
            default:
                RemoveFluentThemeOnWindow(window);
                break;
        }
    }

    private static void ApplyFluentThemeOnApplication(string theme, bool forceUpdate)
    {
        string requestedTheme = theme == "System" ? GetSystemTheme2() : theme;

        if(!_isFluentThemeInitialized)
        {
            InitializeFluentTheme2();
        }

        ApplyTheme2(requestedTheme, DwmColorization.GetSystemAccentColor(), forceUpdate);

        _isFluentThemeEnabled = true;
    }

    private static void RemoveFluentThemeOnApplication()
    {
        if(!IsFluentThemeEnabled)
        {
            return;
        }

        foreach (Window window in Application.Current.Windows)
        {
            if(window == null)
            {
                continue;
            }

            SetImmersiveDarkMode(window, false);
            WindowBackdropManager.SetBackdrop(window, WindowBackdropType.None);
        }

        FindFluentThemeAndColorResourceInApp(out ResourceDictionary themeDictionary, out ResourceDictionary colorDictionary);
        Application.Current.Resources.MergedDictionaries.Remove(themeDictionary);
        Application.Current.Resources.MergedDictionaries.Remove(colorDictionary);

        _isFluentThemeEnabled = false;
        _isFluentThemeInitialized = false;
    }

    private static void ApplyFluentThemeOnWindow(Window window, string theme, bool forceUpdate)
    {
        string requestedTheme = theme == "System" ? GetSystemTheme2() : theme;

        if(!window.IsFluentThemeInitialized)
        {
            InitializeFluentTheme2(window);
        }

        ApplyTheme2(window, requestedTheme, DwmColorization.GetSystemAccentColor(), forceUpdate);

        if(!window.IsFluentThemeEnabled)
        {
            window.IsFluentThemeEnabled = true;
            _fluentEnabledWindows.Add(window);
        }
    }

    private static void RemoveFluentThemeOnWindow(Window window)
    {
        if(!window.IsFluentThemeEnabled)
        {
            return;
        }

        SetImmersiveDarkMode(window, false);
        WindowBackdropManager.SetBackdrop(window, WindowBackdropType.None);

        FindFluentThemeAndColorResourceInWindow(window, out ResourceDictionary themeDictionary, out ResourceDictionary colorDictionary);
        window.Resources.MergedDictionaries.Remove(themeDictionary);
        window.Resources.MergedDictionaries.Remove(colorDictionary);

        window.IsFluentThemeEnabled = false;
        _fluentEnabledWindows.Remove(window);
    }

    private static void ApplyTheme2(string requestedTheme, Color requestedAccentColor, bool forceUpdate = false)
    {
        if(Application.Current == null)
        {
            return;
        }

        if (forceUpdate || requestedTheme != _currentApplicationTheme || requestedAccentColor != DwmColorization.CurrentApplicationAccentColor)
        {
            DwmColorization.UpdateAccentColors(requestedTheme);

            Uri dictionaryUri = GetFluentWindowThemeColorResourceUri(requestedTheme);
            AddOrUpdateThemeResources(dictionaryUri);

            var windows = Application.Current.Windows;
            foreach (Window window in windows)
            {
                if (window == null || window.Theme != "Fluent")
                {
                    continue;
                }

                bool requestedUseLightMode = requestedTheme == "Light";
                SetImmersiveDarkMode(window, !requestedUseLightMode);
                WindowBackdropManager.SetBackdrop(window, SystemParameters.HighContrast ? WindowBackdropType.None : WindowBackdropType.MainWindow);
            }

            _currentApplicationTheme = requestedTheme;
        }
    }

    private static void ApplyTheme2(Window window, string requestedTheme, Color requestedAccentColor, bool forceUpdate = false)
    {
        if (forceUpdate || requestedTheme != window._currentWindowTheme || requestedAccentColor != DwmColorization.CurrentApplicationAccentColor)
        {
            DwmColorization.UpdateAccentColors(window, requestedTheme);

            Uri dictionaryUri = GetFluentWindowThemeColorResourceUri(requestedTheme);
            AddOrUpdateThemeResources(window, dictionaryUri);

            bool requestedUseLightMode = requestedTheme == "Light";
            SetImmersiveDarkMode(window, !requestedUseLightMode);
            WindowBackdropManager.SetBackdrop(window, SystemParameters.HighContrast ? WindowBackdropType.None : WindowBackdropType.MainWindow);
        
            window._currentWindowTheme = requestedTheme;
        }
    }

    private static Uri GetFluentWindowThemeColorResourceUri(string requestedTheme)
    {
        string themeColorFileName = requestedTheme.ToLower() + ".xaml";

        if (SystemParameters.HighContrast)
        {
            themeColorFileName = "hc.xaml";
        }

        return new Uri("pack://application:,,,/PresentationFramework.Fluent;component/Resources/Theme/" + themeColorFileName, UriKind.Absolute);
    }

    private static string GetSystemTheme2()
    {
        if(SystemParameters.HighContrast)
        {
            return "HC";
        }

        return IsSystemThemeLight() ? "Light" : "Dark";
    }

    internal static void InitializeFluentTheme2()
    {
        if(!_isFluentThemeInitialized)
        {
            FindFluentThemeAndColorResourceInApp(out ResourceDictionary themeDictionary, out ResourceDictionary colorDictionary);

            if(themeDictionary == null)
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(themeDictionarySource, UriKind.Absolute) });
            }

            if(colorDictionary == null)
            {
                _currentApplicationTheme = GetSystemTheme2();
                var themeColorResourceUri = GetFluentWindowThemeColorResourceUri(_currentApplicationTheme);

                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = themeColorResourceUri });
            }

            _isFluentThemeInitialized = true;
        }
    }

    internal static void InitializeFluentTheme2(Window window)
    {
        if(!window.IsFluentThemeInitialized)
        {
            FindFluentThemeAndColorResourceInWindow(window, out ResourceDictionary themeDictionary, out ResourceDictionary colorDictionary);

            if(themeDictionary == null)
            {
                window.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(themeDictionarySource, UriKind.Absolute) });
            }

            if(colorDictionary == null)
            {
                _currentApplicationTheme = GetSystemTheme2();
                var themeColorResourceUri = GetFluentWindowThemeColorResourceUri(_currentApplicationTheme);

                window.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = themeColorResourceUri });
            }

            window.IsFluentThemeInitialized = true;
        }
    }

    private static bool FindFluentThemeAndColorResourceInApp(out ResourceDictionary themeDicitonary, out ResourceDictionary colorDictionary)
    {
        themeDicitonary = null;
        colorDictionary = null;

        foreach (ResourceDictionary mergedDictionary in Application.Current.Resources.MergedDictionaries)
        {
            if (mergedDictionary.Source != null)
            {
                var source = mergedDictionary.Source.ToString();
                if (source.EndsWith("Fluent.xaml"))
                {
                    themeDicitonary = mergedDictionary;
                }
                else if (source.EndsWith("Light.xaml") || source.EndsWith("Dark.xaml") || source.EndsWith("HC.xaml"))
                {
                    colorDictionary = mergedDictionary;
                }
            }
        }

        return themeDicitonary != null && colorDictionary != null;
    }

    private static bool FindFluentThemeAndColorResourceInWindow(Window window, out ResourceDictionary themeDicitonary, out ResourceDictionary colorDictionary)
    {
        themeDicitonary = null;
        colorDictionary = null;

        foreach (ResourceDictionary mergedDictionary in window.Resources.MergedDictionaries)
        {
            if (mergedDictionary.Source != null)
            {
                var source = mergedDictionary.Source.ToString();
                if (source.EndsWith("Fluent.xaml"))
                {
                    themeDicitonary = mergedDictionary;
                }
                else if (source.EndsWith("Light.xaml") || source.EndsWith("Dark.xaml") || source.EndsWith("HC.xaml"))
                {
                    colorDictionary = mergedDictionary;
                }
            }
        }

        return themeDicitonary != null && colorDictionary != null;
    }

    private static string GetApplicationThemeFromColorDictionary(ResourceDictionary colorDictionary)
    {
        if(colorDictionary == null)
        {
            return "";
        }

        var colorSource = colorDictionary.Source.ToString();

        if (colorSource.EndsWith("Light.xaml"))
        {
            return "Fluent.Light";
        }
        else if (colorSource.EndsWith("Dark.xaml"))
        {
            return "Fluent.Dark";
        }
        else if (colorSource.EndsWith("HC.xaml"))
        {
            return "Fluent.HC";
        }
        else
        {
            return "";
        }
    }

    private static void AddOrUpdateThemeResources(Window window, Uri dictionaryUri)
    {
        if(window == null || dictionaryUri == null)
        {
            return;
        }

        var newDictionary = new ResourceDictionary() { Source = dictionaryUri };

        ResourceDictionary currentDictionary = window.Resources;
        
        FindFluentThemeAndColorResourceInWindow(window, out _, out ResourceDictionary colorDictionary);

        // currentDictionary.MergedDictionaries.Remove(colorDictionary);
        // currentDictionary.MergedDictionaries.Add(newDictionary);
        
        foreach (var key in newDictionary.Keys)
        {
            if (currentDictionary.Contains(key))
            {
                currentDictionary[key] = newDictionary[key];
            }
            else
            {
                currentDictionary.Add(key, newDictionary[key]);
            }
        }
    }

    internal static bool IsFluentEnabledOnAnyWindow()
    {
        return _fluentEnabledWindows.Count > 0;
    }

    private static string themeDictionarySource = "pack://application:,,,/PresentationFramework.Fluent;component/Resources/Fluent.xaml";

    private static ICollection<Window> _fluentEnabledWindows;
}
