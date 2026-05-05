using System.Windows.Controls;
using DocMind.ViewModels;

namespace DocMind.Views.Pages;

public partial class Page_Categories : Page
{
    public Page_Categories()
    {
        InitializeComponent();
        this.DataContext = new CategoriesViewModel();
    }
}
