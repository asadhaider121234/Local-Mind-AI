using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using DocMind.Models;
using DocMind.Services;

namespace DocMind.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    private static MainWindowViewModel? _instance;
    public static MainWindowViewModel Instance => _instance ??= new MainWindowViewModel();

    private NavigationItem _selectedNavItem = null!;
    private IndexStatus _currentIndexStatus = IndexStatus.Ready;
    private bool _isDarkTheme;
    private bool _isBackendOffline;
    private int _consecutiveFailures = 0;
    private readonly DispatcherTimer _heartbeatTimer;

    public ObservableCollection<NavigationItem> NavItems { get; }

    public NavigationItem SelectedNavItem
    {
        get => _selectedNavItem;
        set
        {
            if (SetProperty(ref _selectedNavItem, value))
            {
                foreach (var item in NavItems)
                {
                    item.IsSelected = (item == value);
                }

                if (value != null)
                {
                    NavigationService.Instance.Navigate(value.TargetPage);
                    // Do NOT trigger a health check on every nav click —
                    // the heartbeat timer handles this on a 15-second cadence.
                }
            }
        }
    }

    public IndexStatus CurrentIndexStatus
    {
        get => _currentIndexStatus;
        set => SetProperty(ref _currentIndexStatus, value);
    }

    public string AppVersion => SettingsService.Instance.Settings.AppVersion;

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                ThemeManager.Instance.Apply(value ? "Dark" : "Light");
            }
        }
    }

    public bool IsBackendOffline
    {
        get => _isBackendOffline;
        set => SetProperty(ref _isBackendOffline, value);
    }

    public SearchViewModel SearchVM { get; }

    public ICommand NavigateCommand { get; }
    public ICommand ExitCommand { get; }

    public MainWindowViewModel()
    {
        _instance = this;
        SearchVM = new SearchViewModel();
        NavItems = new ObservableCollection<NavigationItem>
        {
            new NavigationItem { Id = "search", Label = "Search", Icon = "🔍", TargetPage = "Search" },
            new NavigationItem { Id = "library", Label = "My Library", Icon = "📚", TargetPage = "Library" },
            new NavigationItem { Id = "categories", Label = "Categories", Icon = "📁", TargetPage = "Categories" },
            new NavigationItem { Id = "duplicates", Label = "Duplicates", Icon = "👥", TargetPage = "Duplicates" },
            new NavigationItem { Id = "evaluation", Label = "Evaluation", Icon = "📊", TargetPage = "Evaluation" },
            new NavigationItem { Id = "settings", Label = "Settings", Icon = "⚙️", TargetPage = "Settings" }
        };

        NavigateCommand = new RelayCommand<NavigationItem>(item => SelectedNavItem = item);
        ExitCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());

        // Initialize theme toggle state
        _isDarkTheme = ThemeManager.Instance.IsDark;
        ThemeManager.Instance.ThemeChanged += (theme) => IsDarkTheme = (theme == "Dark");

        // Set initial selected nav
        string initialPage = SettingsService.Instance.LastPage;
        var initialItem = NavItems.FirstOrDefault(n => n.TargetPage == initialPage) ?? NavItems.First();
        
        // Don't trigger setter directly to avoid premature navigation before Frame is ready
        _selectedNavItem = initialItem;
        _selectedNavItem.IsSelected = true;

        // Perform an immediate health check on launch
        CheckBackendHealth();

        // Start a heartbeat timer — checks every 15 seconds.
        // This means the banner will disappear automatically once
        // the backend finishes starting up, without needing a nav click.
        _heartbeatTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _heartbeatTimer.Tick += (_, _) => CheckBackendHealth();
        _heartbeatTimer.Start();
    }

    private async void CheckBackendHealth()
    {
        var health = await ApiService.Instance.CheckHealthAsync();

        if (health != null)
        {
            // Success — reset failure counter and mark online immediately
            _consecutiveFailures = 0;
            IsBackendOffline = false;
            CurrentIndexStatus = health.TotalChunks > 0 ? IndexStatus.Ready : IndexStatus.NotIndexed;
        }
        else
        {
            // Only show the offline banner after 2 consecutive failures.
            // This prevents a single slow response (e.g. backend busy with LLM)
            // from incorrectly flashing the "Backend offline" banner.
            _consecutiveFailures++;
            if (_consecutiveFailures >= 2)
            {
                IsBackendOffline = true;
            }
        }
    }
}
