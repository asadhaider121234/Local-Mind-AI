using System.Windows;
using System.Windows.Controls;
using DocMind.ViewModels;

namespace DocMind.Views.Pages;

public partial class Page_Duplicates : Page
{
    public Page_Duplicates()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Called whenever any file checkbox is checked or unchecked.
    /// Delegates to the ViewModel to recount the global selection totals.
    /// </summary>
    private void OnFileCheckChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is DuplicatesViewModel vm)
            vm.RefreshGlobalSelection();
    }
}
