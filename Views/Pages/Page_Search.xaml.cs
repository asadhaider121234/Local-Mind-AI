using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
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

    private async void CopyAnswer_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SearchViewModel vm) return;

        var answerText = vm.CurrentAnswer?.AnswerText;
        if (string.IsNullOrWhiteSpace(answerText)) return;

        try
        {
            Clipboard.SetText(answerText);
        }
        catch
        {
            // Clipboard may be locked by another process — silently ignore
            return;
        }

        // Visual feedback: swap to "✓ Copied!" briefly
        CopyIcon.Text = "✓";
        CopyIcon.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // green
        CopyLabel.Text = "Copied!";
        CopyLabel.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));

        await Task.Delay(1500);

        // Revert to original look
        CopyIcon.Text = "📋";
        CopyIcon.ClearValue(System.Windows.Controls.TextBlock.ForegroundProperty);
        CopyLabel.Text = "Copy";
        CopyLabel.ClearValue(System.Windows.Controls.TextBlock.ForegroundProperty);
    }
}

