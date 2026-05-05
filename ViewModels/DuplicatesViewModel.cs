using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DocMind.Services;
using DocMind.Models;

namespace DocMind.ViewModels
{
    // ─────────────────────────────────────────────────
    //  DuplicateFileViewModel
    // ─────────────────────────────────────────────────
    public class DuplicateFileViewModel : BaseViewModel
    {
        public string FilePath      { get; set; } = string.Empty;
        public string FileName      { get; set; } = string.Empty;
        public double FileSizeKB    { get; set; }
        public string LastModified  { get; set; } = string.Empty;
        public bool   IsRecommendedKeep { get; set; }

        private bool _isSelectedForDeletion;
        public bool IsSelectedForDeletion
        {
            get => _isSelectedForDeletion;
            set => SetProperty(ref _isSelectedForDeletion, value);
        }

        /// <summary>Formatted size label e.g. "1.2 MB"</summary>
        public string FileSizeLabel => FileSizeKB >= 1024
            ? $"{FileSizeKB / 1024.0:F1} MB"
            : $"{FileSizeKB:F0} KB";
    }

    // ─────────────────────────────────────────────────
    //  DuplicateGroupViewModel
    // ─────────────────────────────────────────────────
    public class DuplicateGroupViewModel : BaseViewModel
    {
        public int    GroupId         { get; set; }
        public string Type            { get; set; } = "Exact";   // "Exact" | "Near"
        public double SimilarityScore { get; set; } = 1.0;       // 0‒1, used for Near-Match groups

        public ObservableCollection<DuplicateFileViewModel> Files { get; } = new();

        // ── Expand / collapse ────────────────────────
        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        // ── Computed helpers ─────────────────────────
        public bool IsExact   => Type == "Exact";
        public bool IsNear    => Type == "Near";

        public string BadgeLabel     => IsExact ? "EXACT" : "NEAR MATCH";
        public string FileSizeLabel  => Files.FirstOrDefault()?.FileSizeLabel ?? "—";
        public string GroupLabel     => $"Group {GroupId}  —  {(IsExact ? "Exact Duplicate" : "Near-Duplicate")}";
        public string SimilarityLabel => $"{SimilarityScore * 100:F0}% similar";

        /// <summary>Excerpt for the left pane of the side-by-side diff view (file 1).</summary>
        public string PreviewLeft  { get; set; } = string.Empty;

        /// <summary>Excerpt for the right pane of the side-by-side diff view (file 2).</summary>
        public string PreviewRight { get; set; } = string.Empty;

        // ── Selection helpers ────────────────────────
        public bool HasAnySelected => Files.Any(f => f.IsSelectedForDeletion);

        public void SelectAllDuplicates()
        {
            foreach (var f in Files.Where(f => !f.IsRecommendedKeep))
                f.IsSelectedForDeletion = true;
            OnPropertyChanged(nameof(HasAnySelected));
        }

        public void DeselectAll()
        {
            foreach (var f in Files)
                f.IsSelectedForDeletion = false;
            OnPropertyChanged(nameof(HasAnySelected));
        }

        public ObservableCollection<DuplicateFileViewModel> SelectedFiles =>
            new(Files.Where(f => f.IsSelectedForDeletion));
    }

    // ─────────────────────────────────────────────────
    //  DuplicatesViewModel  (Page DataContext)
    // ─────────────────────────────────────────────────
    public class DuplicatesViewModel : BaseViewModel
    {
        // ── Source data ───────────────────────────────
        private ObservableCollection<DuplicateGroupViewModel> _exactGroups = new();
        public ObservableCollection<DuplicateGroupViewModel> ExactGroups
        {
            get => _exactGroups;
            private set => SetProperty(ref _exactGroups, value);
        }

        private ObservableCollection<DuplicateGroupViewModel> _nearGroups = new();
        public ObservableCollection<DuplicateGroupViewModel> NearGroups
        {
            get => _nearGroups;
            private set => SetProperty(ref _nearGroups, value);
        }

        // ── Summary stats ────────────────────────────
        private int _totalGroups;
        public int TotalGroups
        {
            get => _totalGroups;
            private set => SetProperty(ref _totalGroups, value);
        }

        private string _recoverableLabel = "0 MB";
        public string RecoverableLabel
        {
            get => _recoverableLabel;
            private set => SetProperty(ref _recoverableLabel, value);
        }

        private int _selectedFilesCount;
        public int SelectedFilesCount
        {
            get => _selectedFilesCount;
            set
            {
                SetProperty(ref _selectedFilesCount, value);
                OnPropertyChanged(nameof(BottomBarLabel));
                OnPropertyChanged(nameof(HasSelection));
            }
        }

        private double _selectedMB;
        public double SelectedMB
        {
            get => _selectedMB;
            set
            {
                SetProperty(ref _selectedMB, value);
                OnPropertyChanged(nameof(BottomBarLabel));
            }
        }

        public bool HasSelection => SelectedFilesCount > 0;

        public string BottomBarLabel =>
            $"{SelectedFilesCount} file{(SelectedFilesCount == 1 ? "" : "s")} selected for deletion  •  {SelectedMB:F1} MB to recover";

        // ── Filter tab ────────────────────────────────
        private string _activeFilter = "All";
        public string ActiveFilter
        {
            get => _activeFilter;
            set
            {
                SetProperty(ref _activeFilter, value);
                OnPropertyChanged(nameof(ShowExactSection));
                OnPropertyChanged(nameof(ShowNearSection));
            }
        }

        public bool ShowExactSection => (ActiveFilter == "All" || ActiveFilter == "Exact") && ExactGroups.Any();
        public bool ShowNearSection  => (ActiveFilter == "All" || ActiveFilter == "Near") && NearGroups.Any();

        public int CountAll   => ExactGroups.Count + NearGroups.Count;
        public int CountExact => ExactGroups.Count;
        public int CountNear  => NearGroups.Count;

        // ── Commands ──────────────────────────────────
        public ICommand ScanCommand             { get; }
        public ICommand FilterAllCommand        { get; }
        public ICommand FilterExactCommand      { get; }
        public ICommand FilterNearCommand       { get; }
        public ICommand ToggleExpandCommand     { get; }
        public ICommand SelectAllDupesCommand   { get; }
        public ICommand DeleteGroupSelectedCommand { get; }
        public ICommand SkipGroupCommand        { get; }
        public ICommand DeleteAllSelectedCommand { get; }
        public ICommand RefreshSelectionCommand { get; }

        public DuplicatesViewModel()
        {
            ScanCommand   = new RelayCommand(async _ => await ScanForDuplicatesAsync());

            FilterAllCommand   = new RelayCommand(_ => ActiveFilter = "All");
            FilterExactCommand = new RelayCommand(_ => ActiveFilter = "Exact");
            FilterNearCommand  = new RelayCommand(_ => ActiveFilter = "Near");

            ToggleExpandCommand = new RelayCommand(param =>
            {
                if (param is DuplicateGroupViewModel g)
                    g.IsExpanded = !g.IsExpanded;
            });

            SelectAllDupesCommand = new RelayCommand(param =>
            {
                if (param is DuplicateGroupViewModel g)
                {
                    g.SelectAllDuplicates();
                    RefreshGlobalSelection();
                }
            });

            DeleteGroupSelectedCommand = new RelayCommand(
                param =>
                {
                    if (param is DuplicateGroupViewModel g)
                        ExecuteDeleteGroup(g);
                },
                param => param is DuplicateGroupViewModel g && g.HasAnySelected);

            SkipGroupCommand = new RelayCommand(param =>
            {
                if (param is DuplicateGroupViewModel g)
                {
                    g.DeselectAll();
                    g.IsExpanded = false;
                    RefreshGlobalSelection();
                }
            });

            DeleteAllSelectedCommand = new RelayCommand(
                _ => ExecuteDeleteAll(),
                _ => HasSelection);

            RefreshSelectionCommand = new RelayCommand(_ => RefreshGlobalSelection());

            _ = ScanForDuplicatesAsync();
        }

        // ─────────────────────────────────────────────
        //  Backend API
        // ─────────────────────────────────────────────
        private async Task ScanForDuplicatesAsync()
        {
            var result = await ApiService.Instance.GetDuplicatesAsync();
            if (result == null) return;

            ExactGroups.Clear();
            NearGroups.Clear();

            int groupId = 1;

            if (result.Exact != null)
            {
                foreach (var groupDto in result.Exact)
                {
                    var group = new DuplicateGroupViewModel
                    {
                        GroupId = groupId++,
                        Type = "Exact",
                        SimilarityScore = 1.0
                    };

                    bool isFirst = true;
                    foreach (var file in groupDto.Files)
                    {
                        group.Files.Add(new DuplicateFileViewModel
                        {
                            FilePath = file.Path,
                            FileName = file.Filename,
                            FileSizeKB = file.SizeKb,
                            LastModified = "N/A",
                            IsRecommendedKeep = isFirst
                        });
                        isFirst = false;
                    }
                    ExactGroups.Add(group);
                }
            }

            if (result.Near != null)
            {
                foreach (var groupDto in result.Near)
                {
                    if (groupDto.Files.Count < 2) continue;
                    var file1 = groupDto.Files[0];
                    var file2 = groupDto.Files[1];

                    var group = new DuplicateGroupViewModel
                    {
                        GroupId = groupId++,
                        Type = "Near",
                        SimilarityScore = groupDto.Similarity,
                        PreviewLeft = "Near match detected",
                        PreviewRight = "Review files below"
                    };

                    group.Files.Add(new DuplicateFileViewModel
                    {
                        FilePath = file1.Path,
                        FileName = file1.Filename,
                        FileSizeKB = file1.SizeKb,
                        LastModified = "N/A",
                        IsRecommendedKeep = true
                    });

                    group.Files.Add(new DuplicateFileViewModel
                    {
                        FilePath = file2.Path,
                        FileName = file2.Filename,
                        FileSizeKB = file2.SizeKb,
                        LastModified = "N/A"
                    });

                    NearGroups.Add(group);
                }
            }

            RefreshStats();
            OnPropertyChanged(nameof(ShowExactSection));
            OnPropertyChanged(nameof(ShowNearSection));
        }

        // ─────────────────────────────────────────────
        //  Selection / stats helpers
        // ─────────────────────────────────────────────
        public void RefreshGlobalSelection()
        {
            var allSelected = ExactGroups.Concat(NearGroups).SelectMany(g => g.Files).Where(f => f.IsSelectedForDeletion).ToList();
            SelectedFilesCount = allSelected.Count;
            SelectedMB = allSelected.Sum(f => f.FileSizeKB) / 1024.0;
        }

        private void RefreshStats()
        {
            TotalGroups = ExactGroups.Count + NearGroups.Count;
            double totalDupKB = ExactGroups.Concat(NearGroups).Sum(g =>
                g.Files.Where(f => !f.IsRecommendedKeep).Sum(f => f.FileSizeKB));
            double totalMB = totalDupKB / 1024.0;
            RecoverableLabel = totalMB >= 1024
                ? $"{totalMB / 1024.0:F1} GB recoverable"
                : $"{totalMB:F1} MB recoverable";

            OnPropertyChanged(nameof(CountAll));
            OnPropertyChanged(nameof(CountExact));
            OnPropertyChanged(nameof(CountNear));
        }

        // ─────────────────────────────────────────────
        //  Delete  (per-group)
        // ─────────────────────────────────────────────
        private void ExecuteDeleteGroup(DuplicateGroupViewModel group)
        {
            var toDelete = group.Files.Where(f => f.IsSelectedForDeletion).ToList();
            if (toDelete.Count == 0) return;

            double mbToDelete = toDelete.Sum(f => f.FileSizeKB) / 1024.0;
            var paths = string.Join("\n  • ", toDelete.Select(f => f.FilePath));
            var msg = $"You are about to permanently delete {toDelete.Count} file{(toDelete.Count > 1 ? "s" : "")} " +
                      $"({mbToDelete:F1} MB). This cannot be undone.\n\n  • {paths}";

            var result = MessageBox.Show(msg, "Confirm Deletion",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result != MessageBoxResult.OK) return;

            _ = DeleteFilesAsync(toDelete.Select(f => f.FilePath).ToList(), group);
        }

        // ─────────────────────────────────────────────
        //  Delete All Selected
        // ─────────────────────────────────────────────
        private void ExecuteDeleteAll()
        {
            var toDelete = ExactGroups.Concat(NearGroups).SelectMany(g => g.Files).Where(f => f.IsSelectedForDeletion).ToList();
            if (toDelete.Count == 0) return;

            double mbToDelete = toDelete.Sum(f => f.FileSizeKB) / 1024.0;
            var paths = string.Join("\n  • ", toDelete.Select(f => f.FilePath));
            var msg = $"You are about to permanently delete {toDelete.Count} file{(toDelete.Count > 1 ? "s" : "")} " +
                      $"({mbToDelete:F1} MB). This cannot be undone.\n\n  • {paths}";

            var result = MessageBox.Show(msg, "Confirm Deletion",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result != MessageBoxResult.OK) return;

            // Group by group and delete
            var byGroup = ExactGroups.Concat(NearGroups).ToDictionary(g => g, g => g.Files.Where(f => f.IsSelectedForDeletion).ToList());
            foreach (var (group, files) in byGroup.Where(kvp => kvp.Value.Count > 0))
                _ = DeleteFilesAsync(files.Select(f => f.FilePath).ToList(), group);
        }

        // ─────────────────────────────────────────────
        //  Backend API call  DELETE /files
        // ─────────────────────────────────────────────
        private async Task DeleteFilesAsync(List<string> paths, DuplicateGroupViewModel group)
        {
            var response = await ApiService.Instance.DeleteFilesAsync(paths);
            
            if (response != null && response.Deleted > 0)
            {
                Application.Current.Dispatcher.Invoke(() => RemoveDeletedFiles(paths, group));
            }
        }

        private void RemoveDeletedFiles(List<string> paths, DuplicateGroupViewModel group)
        {
            var pathSet = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);

            foreach (var f in group.Files.Where(f => pathSet.Contains(f.FilePath)).ToList())
                group.Files.Remove(f);

            // Remove the group itself if only the "keep" file remains or group is empty
            if (group.Files.Count <= 1)
            {
                ExactGroups.Remove(group);
                NearGroups.Remove(group);
            }

            RefreshStats();
            RefreshGlobalSelection();
        }
    }
}
