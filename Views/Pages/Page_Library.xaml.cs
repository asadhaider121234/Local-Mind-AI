using System.Windows.Controls;
using DocMind.ViewModels;

namespace DocMind.Views.Pages;

public partial class Page_Library : Page
{
    public Page_Library()
    {
        InitializeComponent();
        this.DataContext = new LibraryViewModel();
    }
}
