using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Threading.Tasks;
using DocMind.Services;
using DocMind.Models;
using System.Diagnostics;

namespace DocMind.ViewModels
{
    public class CategoriesViewModel : BaseViewModel
    {
        public ObservableCollection<CategoryViewModel> Categories { get; } = new();

        private CategoryViewModel? _selectedCategory;
        public CategoryViewModel? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
                OnPropertyChanged(nameof(IsCategorySelected));
                UpdateFilteredFiles();
            }
        }

        public bool IsCategorySelected => SelectedCategory != null;

        private ICollectionView? _filteredFiles;
        public ICollectionView? FilteredFiles
        {
            get => _filteredFiles;
            private set
            {
                _filteredFiles = value;
                OnPropertyChanged(nameof(FilteredFiles));
            }
        }

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
                FilteredFiles?.Refresh();
            }
        }

        private string _selectedSortOption = "Name";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                _selectedSortOption = value;
                OnPropertyChanged(nameof(SelectedSortOption));
                ApplySorting();
            }
        }

        public ICommand SelectCategoryCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SummarizeCommand { get; }
        public ICommand MoveToCommand { get; }

        public CategoriesViewModel()
        {
            SelectCategoryCommand = new RelayCommand(param => 
            {
                if (param is CategoryViewModel category)
                {
                    SelectedCategory = category;
                }
            });

            OpenCommand = new RelayCommand(param => 
            {
                if (param is FileItemViewModel file)
                {
                    try { Process.Start(new ProcessStartInfo(file.FilePath) { UseShellExecute = true }); }
                    catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
                }
            });

            SummarizeCommand = new RelayCommand(async param => 
            {
                if (param is FileItemViewModel file)
                {
                    var res = await ApiService.Instance.SummarizeAsync(file.FilePath);
                    if (res != null) MessageBox.Show(res.Summary, "AI Summary");
                }
            });

            MoveToCommand = new RelayCommand(param => 
            {
                if (param is FileItemViewModel file)
                {
                    MessageBox.Show($"Move {file.FileName} to another category? (Not yet implemented — backend required)", "Move to...", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });

            _ = LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await ApiService.Instance.GetCategoriesAsync();
            if (categories == null) return;

            Categories.Clear();

            // Setup styling map
            var styling = new Dictionary<string, (string icon, string color)>
            {
                { "Financial", ("💰", "#4CAF50") },
                { "Academic",  ("🎓", "#2196F3") },
                { "Personal",  ("👤", "#9C27B0") },
                { "Legal",     ("⚖️", "#F44336") },
                { "Technical", ("🔧", "#FF9800") },
                { "Uncategorized", ("📁", "#757575") }
            };

            int totalFiles = categories.Values.Sum(c => c.Count);

            foreach (var kvp in categories)
            {
                var style = styling.ContainsKey(kvp.Key) ? styling[kvp.Key] : ("📄", "#607D8B");
                
                var catVm = new CategoryViewModel
                {
                    Name = kvp.Key,
                    Icon = style.Item1,
                    ColorHex = style.Item2,
                    Proportion = totalFiles > 0 ? (double)kvp.Value.Count / totalFiles : 0
                };

                // The overview provides a list of files for the category
                foreach (var f in kvp.Value.Files)
                {
                    catVm.Files.Add(new FileItemViewModel
                    {
                        FileName = f.Filename,
                        FileType = f.Extension.TrimStart('.').ToUpper(),
                        Category = kvp.Key,
                        FileSizeKB = f.Size / 1024.0,
                        FilePath = f.Path
                    });
                }
                
                Categories.Add(catVm);
            }
        }

        private void UpdateFilteredFiles()
        {
            if (SelectedCategory == null)
            {
                FilteredFiles = null;
                return;
            }

            var view = CollectionViewSource.GetDefaultView(SelectedCategory.Files);
            view.Filter = obj =>
            {
                if (obj is not FileItemViewModel f) return false;
                return string.IsNullOrEmpty(SearchQuery) ||
                       f.FileName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
            };

            FilteredFiles = view;
            ApplySorting();
        }

        private void ApplySorting()
        {
            if (FilteredFiles == null) return;

            FilteredFiles.SortDescriptions.Clear();

            switch (SelectedSortOption)
            {
                case "Date":
                    FilteredFiles.SortDescriptions.Add(new SortDescription(nameof(FileItemViewModel.LastModified), ListSortDirection.Descending));
                    break;
                case "Size":
                    FilteredFiles.SortDescriptions.Add(new SortDescription(nameof(FileItemViewModel.FileSizeKB), ListSortDirection.Descending));
                    break;
                case "Name":
                default:
                    FilteredFiles.SortDescriptions.Add(new SortDescription(nameof(FileItemViewModel.FileName), ListSortDirection.Ascending));
                    break;
            }
        }
    }
}
