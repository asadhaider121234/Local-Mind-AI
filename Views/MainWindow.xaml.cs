using DocMind.Services;
using DocMind.ViewModels;
using System.Windows;

namespace DocMind.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Setup Navigation Service
        NavigationService.Instance.Initialize(MainFrame);

        // Responsive handling
        SizeChanged += MainWindow_SizeChanged;
        
        // Initial navigation
        Loaded += (s, e) => {
            if (DataContext is MainWindowViewModel vm && vm.SelectedNavItem != null)
            {
                NavigationService.Instance.Navigate(vm.SelectedNavItem.TargetPage);
            }
        };
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width < 1180)
        {
            SidebarColumn.Width = new GridLength(72);
            BrandText.Visibility = Visibility.Collapsed;
        }
        else
        {
            SidebarColumn.Width = new GridLength(240);
            BrandText.Visibility = Visibility.Visible;
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
