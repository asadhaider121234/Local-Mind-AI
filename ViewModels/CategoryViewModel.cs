using System.Collections.ObjectModel;

namespace DocMind.ViewModels
{
    public class CategoryViewModel : BaseViewModel
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _icon = string.Empty;
        public string Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(nameof(Icon)); }
        }

        private string _colorHex = string.Empty;
        public string ColorHex
        {
            get => _colorHex;
            set { _colorHex = value; OnPropertyChanged(nameof(ColorHex)); }
        }

        private double _proportion;
        public double Proportion
        {
            get => _proportion;
            set { _proportion = value; OnPropertyChanged(nameof(Proportion)); }
        }

        public int FileCount => Files.Count;

        private ObservableCollection<FileItemViewModel> _files = new();
        public ObservableCollection<FileItemViewModel> Files
        {
            get => _files;
            set 
            { 
                _files = value; 
                OnPropertyChanged(nameof(Files)); 
                OnPropertyChanged(nameof(FileCount)); 
            }
        }
    }
}
