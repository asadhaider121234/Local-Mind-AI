using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DocMind.Views.Dialogs;
using Microsoft.Win32;
using DocMind.Services;
using DocMind.Models;
using System.Diagnostics;
using DocMind.Views.Pages;

namespace DocMind.ViewModels
{
    public class LibraryViewModel : BaseViewModel
    {
        private ObservableCollection<FileItemViewModel> _allFiles = new();

        private ObservableCollection<FileItemViewModel> _filteredFiles = new();
        public ObservableCollection<FileItemViewModel> FilteredFiles
        {
            get => _filteredFiles;
            set { _filteredFiles = value; OnPropertyChanged(nameof(FilteredFiles)); }
        }

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set { _filterText = value; OnPropertyChanged(nameof(FilterText)); ApplyFilters(); }
        }

        private string _activeTypeFilter = "All";
        public string ActiveTypeFilter
        {
            get => _activeTypeFilter;
            set { _activeTypeFilter = value; OnPropertyChanged(nameof(ActiveTypeFilter)); ApplyFilters(); }
        }

        private string _selectedSortOption = "Name";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set { _selectedSortOption = value; OnPropertyChanged(nameof(SelectedSortOption)); ApplyFilters(); }
        }

        private bool _isGridView = true;
        public bool IsGridView
        {
            get => _isGridView;
            set { _isGridView = value; OnPropertyChanged(nameof(IsGridView)); OnPropertyChanged(nameof(IsListView)); }
        }
        public bool IsListView => !IsGridView;

        private bool _isIndexing;
        public bool IsIndexing
        {
            get => _isIndexing;
            set { _isIndexing = value; OnPropertyChanged(nameof(IsIndexing)); OnPropertyChanged(nameof(IsNotIndexing)); }
        }
        public bool IsNotIndexing => !IsIndexing;

        private int _indexedCount;
        public int IndexedCount
        {
            get => _indexedCount;
            set { _indexedCount = value; OnPropertyChanged(nameof(IndexedCount)); }
        }
        private string _currentFile = string.Empty;
        public string CurrentFile
        {
            get => _currentFile;
            set { _currentFile = value; OnPropertyChanged(nameof(CurrentFile)); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        private int _totalIndexedCount;
        public int TotalIndexedCount
        {
            get => _totalIndexedCount;
            set { _totalIndexedCount = value; OnPropertyChanged(nameof(TotalIndexedCount)); }
        }

        // Stats
        public int CountAll => _allFiles.Count;
        public int CountPDF => _allFiles.Count(f => f.FileType == "PDF");
        public int CountWord => _allFiles.Count(f => f.FileType == "DOCX");
        public int CountExcel => _allFiles.Count(f => f.FileType == "XLSX");
        public int CountImages => _allFiles.Count(f => f.FileType == "Image");
        public int CountCode => _allFiles.Count(f => f.FileType == "Code");
        public int CountPowerPoint => _allFiles.Count(f => f.FileType == "PowerPoint");
        public int CountText => _allFiles.Count(f => f.FileType == "Text");

        public string LastIndexedTime { get; set; } = "Today at 2:30 PM";

        // Commands
        public ICommand IndexFolderCommand { get; }
        public ICommand FilterCategoryCommand { get; }
        public ICommand ToggleViewCommand { get; }
        public ICommand SummarizeCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand FindSimilarCommand { get; }
        public ICommand ChatWithFileCommand { get; }

        public LibraryViewModel()
        {
            IndexFolderCommand = new RelayCommand(async _ => await IndexFolderAsync());
            FilterCategoryCommand = new RelayCommand(param => { ActiveTypeFilter = param?.ToString() ?? "All"; });
            ToggleViewCommand = new RelayCommand(param => { IsGridView = (param?.ToString() == "Grid"); });
            SummarizeCommand = new RelayCommand(param => _ = SummarizeFileAsync(param as FileItemViewModel));
            OpenCommand = new RelayCommand(param => OpenFile(param as FileItemViewModel));
            FindSimilarCommand = new RelayCommand(param => FindSimilar(param as FileItemViewModel));
            ChatWithFileCommand = new RelayCommand(param => ChatWithFile(param as FileItemViewModel));

            _ = LoadFilesAsync();
        }

        private async Task LoadFilesAsync()
        {
            IsLoading = true;
            _allFiles.Clear();
            FilteredFiles.Clear();

            var files = await ApiService.Instance.GetFilesAsync();
            if (files != null)
            {
                foreach (var f in files)
                {
                    string fileType = GetFileType(f.Extension);
                    _allFiles.Add(new FileItemViewModel
                    {
                        FileName = f.Filename,
                        FileType = fileType,
                        Category = f.Category,
                        FileSizeKB = f.Size / 1024.0,
                        PageCount = f.ChunkCount, // Using chunks as an approximation for pages/size
                        LastModified = DateTimeOffset.FromUnixTimeSeconds((long)f.Modified).ToLocalTime().ToString("MMM dd, yyyy"),
                        FilePath = f.Path
                    });
                }
            }
            
            ApplyFilters();
            IsLoading = false;
        }

        private string GetFileType(string ext)
        {
            ext = ext.ToLower();
            if (ext == ".pdf") return "PDF";
            if (ext == ".docx" || ext == ".doc") return "DOCX";
            if (ext == ".xlsx" || ext == ".xls" || ext == ".csv") return "XLSX";
            if (ext == ".ppt" || ext == ".pptx" || ext == ".odp") return "PowerPoint";
            if (ext == ".txt" || ext == ".md" || ext == ".log" || ext == ".rtf") return "Text";
            if (ext == ".jpg" || ext == ".png" || ext == ".jpeg") return "Image";
            if (ext == ".py" || ext == ".cs" || ext == ".cpp" || ext == ".js" || ext == ".html") return "Code";
            return "Other";
        }

        private void ApplyFilters()
        {
            var query = _allFiles.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                query = query.Where(f => f.FileName.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
            }

            if (ActiveTypeFilter != "All")
            {
                query = query.Where(f => f.FileType.Equals(ActiveTypeFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Sorting
            query = SelectedSortOption switch
            {
                "Date" => query.OrderByDescending(f => f.LastModified), // basic string sort for mock, ideally real Date
                "Size" => query.OrderByDescending(f => f.FileSizeKB),
                "Category" => query.OrderBy(f => f.Category).ThenBy(f => f.FileName),
                _ => query.OrderBy(f => f.FileName)
            };

            FilteredFiles = new ObservableCollection<FileItemViewModel>(query);

            // Update stats
            OnPropertyChanged(nameof(CountAll));
            OnPropertyChanged(nameof(CountPDF));
            OnPropertyChanged(nameof(CountWord));
            OnPropertyChanged(nameof(CountExcel));
            OnPropertyChanged(nameof(CountImages));
            OnPropertyChanged(nameof(CountCode));
            OnPropertyChanged(nameof(CountPowerPoint));
            OnPropertyChanged(nameof(CountText));
        }

        private async Task IndexFolderAsync()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder to Index"
            };

            if (dialog.ShowDialog() == true)
            {
                IsIndexing = true;
                
                // Fire and forget the indexing task to the API
                var response = await ApiService.Instance.StartIndexingAsync(dialog.FolderName);
                
                if (response != null && response.IsSuccessStatusCode)
                {
                    _ = PollIndexingStatusAsync();
                }
                else if (response != null && response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    MessageBox.Show("Indexing is already in progress. Please wait for the current task to finish.", "Busy", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Still try to poll if it's already running
                    _ = PollIndexingStatusAsync();
                }
                else
                {
                    MessageBox.Show("Failed to start indexing. Check the backend connection.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsIndexing = false;
                }
            }
        }

        private async Task PollIndexingStatusAsync()
        {
            IsIndexing = true;
            while (IsIndexing)
            {
                var status = await ApiService.Instance.GetIndexingStatusAsync();
                if (status != null)
                {
                    IndexedCount = status.Progress;
                    TotalIndexedCount = status.Total;
                    CurrentFile = status.CurrentFile;
                    IsIndexing = status.IsIndexing;

                    if (!IsIndexing)
                    {
                        // Final refresh
                        await LoadFilesAsync();
                        LastIndexedTime = "Just now";
                        OnPropertyChanged(nameof(LastIndexedTime));
                        break;
                    }
                }
                else
                {
                    // Error polling, assume stopped or failed
                    IsIndexing = false;
                    break;
                }

                await Task.Delay(1000); // Poll every second
            }
        }

        private async Task SummarizeFileAsync(FileItemViewModel? file)
        {
            if (file == null) return;
            
            file.IsLoading = true;
            try
            {
                var result = await ApiService.Instance.SummarizeAsync(file.FilePath);
                if (result != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var dialog = new SummaryDialog(file.FileName, result.Summary)
                        {
                            Owner = Application.Current.MainWindow
                        };
                        dialog.ShowDialog();
                    });
                }
                else
                {
                    MessageBox.Show("Failed to generate summary. Is the backend running?", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                file.IsLoading = false;
            }
        }

        private void OpenFile(FileItemViewModel? file)
        {
            if (file == null || string.IsNullOrEmpty(file.FilePath)) return;
            
            file.IsLoading = true;
            try
            {
                Process.Start(new ProcessStartInfo(file.FilePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            // Brief delay for feedback
            _ = Task.Delay(1000).ContinueWith(_ => file.IsLoading = false);
        }

        private void FindSimilar(FileItemViewModel? file)
        {
            if (file == null) return;
            
            file.IsLoading = true;
            // Navigate to Duplicates page
            NavigationService.Instance.Navigate("Duplicates");
            
            _ = Task.Delay(500).ContinueWith(_ => file.IsLoading = false);
        }

        private void ChatWithFile(FileItemViewModel? file)
        {
            if (file == null) return;
            
            file.IsLoading = true;
            // Navigate to Search page and set context
            var searchVM = MainWindowViewModel.Instance.SearchVM;
            searchVM.SetFileContext(file.FilePath, file.FileName);
            NavigationService.Instance.Navigate("Search");
            
            _ = Task.Delay(500).ContinueWith(_ => file.IsLoading = false);
        }
    }
}
