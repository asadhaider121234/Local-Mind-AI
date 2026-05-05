using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DocMind.Services;
using DocMind.Models;

namespace DocMind.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
            }
        }

        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged(nameof(IsSearching));
            }
        }

        private bool _hasSearchedOnce;
        public bool HasSearchedOnce
        {
            get => _hasSearchedOnce;
            set
            {
                _hasSearchedOnce = value;
                OnPropertyChanged(nameof(HasSearchedOnce));
                OnPropertyChanged(nameof(ShowEmptyState));
                OnPropertyChanged(nameof(ShowResults));
            }
        }

        public bool ShowEmptyState => ChatHistory.Count == 0 && !HasSearchedOnce && !IsSearching;
        public bool ShowResults => HasSearchedOnce && !IsSearching;
        public bool HasHistory => ChatHistory.Count > 0;

        private int _totalFiles = 0;
        public int TotalFiles
        {
            get => _totalFiles;
            set
            {
                _totalFiles = value;
                OnPropertyChanged(nameof(TotalFiles));
            }
        }

        private int _indexedChunks = 0;
        public int IndexedChunks
        {
            get => _indexedChunks;
            set
            {
                _indexedChunks = value;
                OnPropertyChanged(nameof(IndexedChunks));
            }
        }

        private string _lastQueryTime = "—";
        public string LastQueryTime
        {
            get => _lastQueryTime;
            set
            {
                _lastQueryTime = value;
                OnPropertyChanged(nameof(LastQueryTime));
            }
        }

        private string _lastIndexed = "Never";
        public string LastIndexed
        {
            get => _lastIndexed;
            set
            {
                _lastIndexed = value;
                OnPropertyChanged(nameof(LastIndexed));
            }
        }

        private AnswerViewModel? _currentAnswer;
        public AnswerViewModel? CurrentAnswer
        {
            get => _currentAnswer;
            set
            {
                _currentAnswer = value;
                OnPropertyChanged(nameof(CurrentAnswer));
            }
        }

        public ObservableCollection<SearchResultViewModel> Results { get; } = new ObservableCollection<SearchResultViewModel>();
        public ObservableCollection<ChatMessage> ChatHistory { get; } = new();
        public ObservableCollection<string> Suggestions { get; } = new ObservableCollection<string>();

        public ICommand SearchCommand { get; }
        public ICommand SuggestionCommand { get; }
        public ICommand ClearChatCommand { get; }
        public ICommand ClearContextCommand { get; }
        public ICommand StopCommand { get; }

        private CancellationTokenSource? _cts;

        private string? _fileContextPath;
        private string? _fileContextName;
        public string? FileContextName
        {
            get => _fileContextName;
            set { _fileContextName = value; OnPropertyChanged(nameof(FileContextName)); OnPropertyChanged(nameof(IsInFileContext)); }
        }
        public bool IsInFileContext => !string.IsNullOrEmpty(_fileContextPath);

        public SearchViewModel()
        {
            LoadChatHistory();
            _ = LoadStatsAsync();

            SearchCommand = new RelayCommand(async _ => await SearchAsync(SearchQuery), _ => !string.IsNullOrWhiteSpace(SearchQuery) && !IsSearching);
            SuggestionCommand = new RelayCommand(async (param) => 
            {
                if (param is string suggestion)
                {
                    SearchQuery = suggestion;
                    await SearchAsync(suggestion);
                }
            });
            ClearChatCommand = new RelayCommand(_ => ClearChat());
            ClearContextCommand = new RelayCommand(_ => ClearFileContext());
            StopCommand = new RelayCommand(_ => StopSearch());
        }

        private void StopSearch()
        {
            _cts?.Cancel();
        }

        public void SetFileContext(string path, string name)
        {
            _fileContextPath = path;
            FileContextName = name;
            // Clear current chat history when switching to a specific file to avoid confusion? 
            // Or maybe just let it continue. Let's keep history but add a notice.
        }

        public void ClearFileContext()
        {
            _fileContextPath = null;
            FileContextName = null;
        }

        private async Task LoadStatsAsync()
        {
            var health = await ApiService.Instance.CheckHealthAsync();
            if (health != null)
            {
                IndexedChunks = health.TotalChunks;
            }

            var files = await ApiService.Instance.GetFilesAsync();
            if (files != null && files.Count > 0)
            {
                TotalFiles = files.Count;
                double maxModified = 0;
                foreach (var f in files)
                {
                    if (f.Modified > maxModified)
                    {
                        maxModified = f.Modified;
                    }
                }
                
                if (maxModified > 0)
                {
                    try
                    {
                        var dt = DateTimeOffset.FromUnixTimeSeconds((long)maxModified).LocalDateTime;
                        LastIndexed = FormatTimestamp(dt);
                    }
                    catch
                    {
                        LastIndexed = "Unknown";
                    }
                }
                
                // Generate dynamic suggestions based on indexed files
                App.Current.Dispatcher.Invoke(() => 
                {
                    Suggestions.Clear();
                    var random = new Random();
                    var shuffledFiles = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderBy(files, x => random.Next()), 4));
                    
                    if (shuffledFiles.Count > 0) Suggestions.Add($"Summarize {shuffledFiles[0].Filename}");
                    if (shuffledFiles.Count > 1) Suggestions.Add($"What is {shuffledFiles[1].Filename} about?");
                    if (shuffledFiles.Count > 2) Suggestions.Add($"Key points in {shuffledFiles[2].Filename}");
                    if (shuffledFiles.Count > 3) Suggestions.Add($"Details from {shuffledFiles[3].Filename}");
                });
            }
            else
            {
                App.Current.Dispatcher.Invoke(() => 
                {
                    Suggestions.Clear();
                    Suggestions.Add("How do I use DocMind?");
                    Suggestions.Add("What file types are supported?");
                    Suggestions.Add("Explain local AI");
                    Suggestions.Add("Help me get started");
                });
            }
        }

        public static string FormatTimestamp(DateTime dt)
        {
            if (dt.Date == DateTime.Today)
                return "Today, " + dt.ToString("h:mm tt");
            if (dt.Date == DateTime.Today.AddDays(-1))
                return "Yesterday, " + dt.ToString("h:mm tt");
            return dt.ToString("MMM d, h:mm tt");
        }

        private void LoadChatHistory()
        {
            var saved = ChatHistoryService.Instance.LoadHistory();
            ChatHistory.Clear();
            foreach (var msg in saved)
                ChatHistory.Add(msg);

            // If history exists, restore the last answer 
            // and sources to the results area
            var lastAssistant = System.Linq.Enumerable.LastOrDefault(saved, m => m.Role == "assistant");
            if (lastAssistant != null)
            {
                var sourceRefs = new System.Collections.Generic.List<SourceReference>();
                if (lastAssistant.Sources != null)
                {
                    foreach (var s in lastAssistant.Sources)
                    {
                        sourceRefs.Add(new SourceReference { FileName = s.Filename, PageNumber = s.Page });
                        Results.Add(new SearchResultViewModel
                        {
                            FileName = s.Filename,
                            PageNumber = s.Page,
                            Category = s.Category,
                            Excerpt = s.Excerpt,
                            RelevanceScore = s.Score,
                            FilePath = s.Filename
                        });
                    }
                }

                CurrentAnswer = new AnswerViewModel
                {
                    AnswerText = lastAssistant.Content,
                    Sources = sourceRefs
                };
                
                HasSearchedOnce = true;
                OnPropertyChanged(nameof(HasHistory));
                OnPropertyChanged(nameof(ShowEmptyState));
                OnPropertyChanged(nameof(ShowResults));
            }
            OnPropertyChanged(nameof(HasHistory));
            OnPropertyChanged(nameof(ShowEmptyState));
        }

        private void ClearChat()
        {
            ChatHistory.Clear();
            ChatHistoryService.Instance.ClearHistory();
            CurrentAnswer = null;
            HasSearchedOnce = false;
            Results.Clear();
            OnPropertyChanged(nameof(HasHistory));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowResults));
        }

        public async Task SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            IsSearching = true;
            HasSearchedOnce = false;
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowResults));
            
            var userMsg = new ChatMessage
            {
                Role = "user",
                Content = query,
                Timestamp = DateTime.Now,
                Mode = "user"
            };
            ChatHistory.Add(userMsg);
            OnPropertyChanged(nameof(HasHistory));
            OnPropertyChanged(nameof(ShowEmptyState));
            
            _cts = new CancellationTokenSource();
            
            try
            {
                var result = await ApiService.Instance.QueryAsync(query, _fileContextPath, _cts.Token);
                Results.Clear();

                if (result == null)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        CurrentAnswer = new AnswerViewModel { AnswerText = "Query cancelled by user." };
                    }
                    else
                    {
                        CurrentAnswer = new AnswerViewModel
                        {
                            AnswerText = "⏳ The AI is still thinking (Phi-3.5 runs on CPU and may take 2–4 minutes). If this keeps happening, ensure the DocMind backend is running at http://127.0.0.1:8000."
                        };
                    }
                }
                else
                {
                    var sourceRefs = new System.Collections.Generic.List<SourceReference>();
                    foreach (var s in result.Sources)
                    {
                        sourceRefs.Add(new SourceReference { FileName = s.Filename, PageNumber = s.Page });
                        Results.Add(new SearchResultViewModel
                        {
                            FileName = s.Filename,
                            PageNumber = s.Page,
                            Category = s.Category,
                            Excerpt = s.Excerpt,
                            RelevanceScore = s.Score,
                            FilePath = s.Filename
                        });
                    }

                    CurrentAnswer = new AnswerViewModel
                    {
                        AnswerText = result.Answer,
                        Sources = sourceRefs
                    };

                    LastQueryTime = $"{result.LatencyMs / 1000.0:F1}s";
                    
                    var assistantMsg = new ChatMessage
                    {
                        Role = "assistant",
                        Content = result.Answer,
                        Timestamp = DateTime.Now,
                        Mode = "document",
                        Sources = result.Sources
                    };
                    ChatHistory.Add(assistantMsg);
                }
            }
            catch (OperationCanceledException)
            {
                CurrentAnswer = new AnswerViewModel { AnswerText = "Query cancelled." };
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }

            ChatHistoryService.Instance.SaveHistory(ChatHistory);

            IsSearching = false;
            HasSearchedOnce = true;
            SearchQuery = string.Empty;
            OnPropertyChanged(nameof(HasHistory));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowResults));
        }
    }
}
