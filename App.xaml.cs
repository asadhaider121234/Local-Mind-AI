using DocMind.Services;
using DocMind.ViewModels;
using DocMind.Views;
using System.Windows;

namespace DocMind;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        var themeManager = ThemeManager.Instance;
        var navigationService = NavigationService.Instance;

        // Apply saved theme preference
        themeManager.ApplySavedTheme();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
