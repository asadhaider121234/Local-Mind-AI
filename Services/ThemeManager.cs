using System.Windows;

namespace DocMind.Services;

public class ThemeManager
{
    private static ThemeManager? _instance;
    public static ThemeManager Instance => _instance ??= new ThemeManager();

    private const string DarkThemeUri  = "Themes/DarkTheme.xaml";
    private const string LightThemeUri = "Themes/LightTheme.xaml";

    public string CurrentTheme { get; private set; } = "Dark";

    public event Action<string>? ThemeChanged;

    public void ApplySavedTheme()
    {
        var saved = SettingsService.Instance.Theme;
        Apply(saved);
    }

    public void ToggleTheme()
    {
        Apply(CurrentTheme == "Dark" ? "Light" : "Dark");
    }

    public void Apply(string themeName)
    {
        CurrentTheme = themeName;
        var uri = themeName == "Light" ? LightThemeUri : DarkThemeUri;

        var merged = Application.Current.Resources.MergedDictionaries;

        // Remove existing theme dictionary (index 0 by convention)
        if (merged.Count > 0) merged.RemoveAt(0);

        var dict = new ResourceDictionary
        {
            Source = new Uri(uri, UriKind.Relative)
        };
        merged.Insert(0, dict);

        SettingsService.Instance.Theme = themeName;
        ThemeChanged?.Invoke(themeName);
    }

    public bool IsDark => CurrentTheme == "Dark";
}
