using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DocMind.Services;
using DocMind.Models;

namespace DocMind.ViewModels
{
    public class EvaluationViewModel : BaseViewModel
    {
        // ── Card Metrics ──────────────────────────────
        private double _precision = 0.0;
        public double Precision { get => _precision; set => SetProperty(ref _precision, value); }

        private double _recall = 0.0;
        public double Recall { get => _recall; set => SetProperty(ref _recall, value); }

        private double _mrr = 0.0;
        public double MRR { get => _mrr; set => SetProperty(ref _mrr, value); }

        private double _avgLatency = 0.0;
        public double AvgLatency { get => _avgLatency; set => SetProperty(ref _avgLatency, value); }

        // ── Latency Breakdown (Seconds) ────────────────
        private double _embeddingTime = 0.0;
        public double EmbeddingTime { get => _embeddingTime; set => SetProperty(ref _embeddingTime, value); }

        private double _retrievalTime = 0.0;
        public double RetrievalTime { get => _retrievalTime; set => SetProperty(ref _retrievalTime, value); }

        private double _generationTime = 0.0;
        public double GenerationTime { get => _generationTime; set => SetProperty(ref _generationTime, value); }

        // ── Comparison Data ───────────────────────────
        public double SemanticPrecision { get; private set; } = 0.0;
        public double SemanticRecall    { get; private set; } = 0.0;
        public double SemanticMRR       { get; private set; } = 0.0;

        public double BM25Precision { get; private set; } = 0.0;
        public double BM25Recall    { get; private set; } = 0.0;
        public double BM25MRR       { get; private set; } = 0.0;

        // ── Running State ─────────────────────────────
        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                    OnPropertyChanged(nameof(IsNotRunning));
            }
        }
        public bool IsNotRunning => !IsRunning;

        private int _progressValue;
        public int ProgressValue { get => _progressValue; set => SetProperty(ref _progressValue, value); }

        private string _currentQuery = string.Empty;
        public string CurrentQuery { get => _currentQuery; set => SetProperty(ref _currentQuery, value); }

        public int TotalQueries => 20; // 20 benchmark queries in backend
        public string LastRunText { get; private set; } = "Not yet run";

        // ── Commands ──────────────────────────────────
        public ICommand RunBenchmarkCommand { get; }

        public EvaluationViewModel()
        {
            RunBenchmarkCommand = new RelayCommand(_ => ExecuteBenchmark());
        }

        private async void ExecuteBenchmark()
        {
            if (IsRunning) return;

            IsRunning = true;
            ProgressValue = 0;
            CurrentQuery = "Running full benchmark. This may take a while...";

            // Start fake progress while waiting
            bool isDone = false;
            _ = Task.Run(async () =>
            {
                while (!isDone && ProgressValue < 90)
                {
                    await Task.Delay(500);
                    Application.Current.Dispatcher.Invoke(() => ProgressValue += 1);
                }
            });

            var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(120));
            try
            {
                var result = await ApiService.Instance.EvaluateAsync(TotalQueries, cts.Token);
                isDone = true;

                if (result != null)
                {
                    ProgressValue = 100;
                    CurrentQuery = "Benchmark complete!";

                    Precision = result.PrecisionAt5;
                    Recall = result.RecallAt5;
                    MRR = result.Mrr;
                    AvgLatency = result.AvgLatencyMs / 1000.0;

                    // Real per-step timing from backend instrumentation
                    EmbeddingTime  = result.EmbeddingMs  / 1000.0;
                    RetrievalTime  = result.RetrievalMs  / 1000.0;
                    GenerationTime = result.GenerationMs / 1000.0;

                    SemanticPrecision = result.PrecisionAt5;
                    SemanticRecall = result.RecallAt5;
                    SemanticMRR = result.Mrr;

                    BM25Precision = result.SemanticVsKeyword?.Keyword?.P5 ?? 0;
                    BM25Recall = result.SemanticVsKeyword?.Keyword?.R5 ?? 0;
                    BM25MRR = result.SemanticVsKeyword?.Keyword?.Mrr ?? 0;

                    LastRunText = DateTime.Now.ToString("dd MMM yyyy, h:mm tt");

                    // Notify UI of property changes
                    OnPropertyChanged(nameof(EmbeddingTime));
                    OnPropertyChanged(nameof(RetrievalTime));
                    OnPropertyChanged(nameof(GenerationTime));
                    OnPropertyChanged(nameof(EmbeddingWidth));
                    OnPropertyChanged(nameof(RetrievalWidth));
                    OnPropertyChanged(nameof(GenerationWidth));
                    OnPropertyChanged(nameof(SemanticPrecision));
                    OnPropertyChanged(nameof(SemanticRecall));
                    OnPropertyChanged(nameof(SemanticMRR));
                    OnPropertyChanged(nameof(BM25Precision));
                    OnPropertyChanged(nameof(BM25Recall));
                    OnPropertyChanged(nameof(BM25MRR));
                    OnPropertyChanged(nameof(LastRunText));
                }
                else
                {
                    CurrentQuery = "Benchmark failed.";
                    MessageBox.Show("Failed to run evaluation. Check backend connection.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException)
            {
                isDone = true;
                CurrentQuery = "Evaluation timed out. Try fewer queries.";
                ProgressValue = 0;
            }
            catch (Exception ex)
            {
                isDone = true;
                CurrentQuery = $"Evaluation failed: {ex.Message}";
                ProgressValue = 0;
            }

            IsRunning = false;
        }

        // ── Helper for bar widths ──────────────────────
        // In a real app, these would be normalized to a max width
        public double EmbeddingWidth => EmbeddingTime * 100;
        public double RetrievalWidth => RetrievalTime * 100;
        public double GenerationWidth => GenerationTime * 100;
    }
}
