namespace System.Windows;

internal class FluentThemeState
{
    public FluentThemeState(string themeName, bool useLightColors, Color accentColor = null)
    {
        _themeName = themeName;
        _useLightColors = useLightColors;
        _accentColor = accentColor;
        _isActive = true;
    }

    public FluentThemeState()
    {
        _isActive = false;
    }

    public string ThemeName => _themeName;
    public bool UseLightColors => _useLightColors;
    public Color AccentColor => _accentColor;

    private string _themeName;
    private bool _useLightColors;
    private Color _accentColor;
}

