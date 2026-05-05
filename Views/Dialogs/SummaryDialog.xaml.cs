using System.Windows;

namespace DocMind.Views.Dialogs
{
    public partial class SummaryDialog : Window
    {
        public SummaryDialog(string fileName, string summary)
        {
            InitializeComponent();
            FileNameText.Text = fileName;
            SummaryText.Text = summary;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
