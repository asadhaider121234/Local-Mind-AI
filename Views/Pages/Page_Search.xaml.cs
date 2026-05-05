using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DocMind.ViewModels;

namespace DocMind.Views.Pages;

public partial class Page_Search : Page
{
    public Page_Search()
    {
        InitializeComponent();
        this.DataContext = MainWindowViewModel.Instance.SearchVM;
        this.Loaded += Page_Search_Loaded;
    }

    private void Page_Search_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SearchViewModel vm)
        {
            vm.ChatHistory.CollectionChanged -= ChatHistory_CollectionChanged;
            vm.ChatHistory.CollectionChanged += ChatHistory_CollectionChanged;
        }
    }

    private void ChatHistory_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            ChatScrollViewer.ScrollToBottom();
        }
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is SearchViewModel vm)
            {
                if (vm.SearchCommand.CanExecute(null))
                {
                    vm.SearchCommand.Execute(null);
                }
            }
        }
    }
}
