using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DocMind.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private string _theme = "Dark";
        public string Theme { get => _theme; set => SetProperty(ref _theme, value); }

        private string _lastPage = "Search";
        public string LastPage { get => _lastPage; set => SetProperty(ref _lastPage, value); }

        private string _appVersion = "v1.0";
        public string AppVersion { get => _appVersion; set => SetProperty(ref _appVersion, value); }

        // ── Model Settings ───────────────────────────
        private string _llmModelPath = "models/phi-3.5.gguf";
        public string LlmModelPath { get => _llmModelPath; set => SetProperty(ref _llmModelPath, value); }

        private string _visionModel = "BLIP-2 (Recommended)";
        public string VisionModel { get => _visionModel; set => SetProperty(ref _visionModel, value); }

        private int _cpuThreads = 4;
        public int CpuThreads { get => _cpuThreads; set => SetProperty(ref _cpuThreads, value); }

        private int _contextLength = 4096;
        public int ContextLength { get => _contextLength; set => SetProperty(ref _contextLength, value); }

        // ── Indexing Settings ────────────────────────
        private List<string> _watchedFolders = new();
        public List<string> WatchedFolders { get => _watchedFolders; set => SetProperty(ref _watchedFolders, value); }

        private int _chunkSize = 400;
        public int ChunkSize { get => _chunkSize; set => SetProperty(ref _chunkSize, value); }

        private int _chunkOverlap = 80;
        public int ChunkOverlap { get => _chunkOverlap; set => SetProperty(ref _chunkOverlap, value); }

        private bool _autoReindex = true;
        public bool AutoReindex { get => _autoReindex; set => SetProperty(ref _autoReindex, value); }
        
        private bool _indexPdf = true;
        public bool IndexPdf { get => _indexPdf; set => SetProperty(ref _indexPdf, value); }

        private bool _indexDocx = true;
        public bool IndexDocx { get => _indexDocx; set => SetProperty(ref _indexDocx, value); }

        private bool _indexXlsx = true;
        public bool IndexXlsx { get => _indexXlsx; set => SetProperty(ref _indexXlsx, value); }

        private bool _indexTxt = true;
        public bool IndexTxt { get => _indexTxt; set => SetProperty(ref _indexTxt, value); }

        private bool _indexMd = true;
        public bool IndexMd { get => _indexMd; set => SetProperty(ref _indexMd, value); }

        private bool _indexLog = true;
        public bool IndexLog { get => _indexLog; set => SetProperty(ref _indexLog, value); }

        private bool _indexRtf = true;
        public bool IndexRtf { get => _indexRtf; set => SetProperty(ref _indexRtf, value); }

        private bool _indexPptx = true;
        public bool IndexPptx { get => _indexPptx; set => SetProperty(ref _indexPptx, value); }

        private bool _indexCsv = true;
        public bool IndexCsv { get => _indexCsv; set => SetProperty(ref _indexCsv, value); }

        private bool _indexJpg = true;
        public bool IndexJpg { get => _indexJpg; set => SetProperty(ref _indexJpg, value); }

        private bool _indexPng = true;
        public bool IndexPng { get => _indexPng; set => SetProperty(ref _indexPng, value); }

        private bool _indexPY = true;
        public bool IndexPY { get => _indexPY; set => SetProperty(ref _indexPY, value); }

        private bool _indexCS = true;
        public bool IndexCS { get => _indexCS; set => SetProperty(ref _indexCS, value); }

        private bool _indexCPP = true;
        public bool IndexCPP { get => _indexCPP; set => SetProperty(ref _indexCPP, value); }

        private bool _indexJS = true;
        public bool IndexJS { get => _indexJS; set => SetProperty(ref _indexJS, value); }

        private bool _indexHTML = true;
        public bool IndexHTML { get => _indexHTML; set => SetProperty(ref _indexHTML, value); }

        private bool _indexCSS = true;
        public bool IndexCSS { get => _indexCSS; set => SetProperty(ref _indexCSS, value); }

        private bool _indexJSON = true;
        public bool IndexJSON { get => _indexJSON; set => SetProperty(ref _indexJSON, value); }

        private bool _indexXML = true;
        public bool IndexXML { get => _indexXML; set => SetProperty(ref _indexXML, value); }

        private bool _indexSQL = true;
        public bool IndexSQL { get => _indexSQL; set => SetProperty(ref _indexSQL, value); }

        private bool _indexXAML = true;
        public bool IndexXAML { get => _indexXAML; set => SetProperty(ref _indexXAML, value); }

        private bool _indexIPYNB = true;
        public bool IndexIPYNB { get => _indexIPYNB; set => SetProperty(ref _indexIPYNB, value); }

        // ── Search Settings ──────────────────────────
        private int _resultsPerQuery = 5;
        public int ResultsPerQuery { get => _resultsPerQuery; set => SetProperty(ref _resultsPerQuery, value); }

        private double _minRelevanceScore = 0.3;
        public double MinRelevanceScore { get => _minRelevanceScore; set => SetProperty(ref _minRelevanceScore, value); }

        private bool _enableHallucinationFilter = true;
        public bool EnableHallucinationFilter { get => _enableHallucinationFilter; set => SetProperty(ref _enableHallucinationFilter, value); }

        private bool _enableImageSearch = false;
        public bool EnableImageSearch { get => _enableImageSearch; set => SetProperty(ref _enableImageSearch, value); }

        // ── Appearance ───────────────────────────────
        private string _fontSize = "Medium";
        public string FontSize { get => _fontSize; set => SetProperty(ref _fontSize, value); }

        private bool _showRelevanceScores = true;
        public bool ShowRelevanceScores { get => _showRelevanceScores; set => SetProperty(ref _showRelevanceScores, value); }

        private bool _showFilePaths = true;
        public bool ShowFilePaths { get => _showFilePaths; set => SetProperty(ref _showFilePaths, value); }

        // ── Backend / API ────────────────────────────
        private string _apiHost = "127.0.0.1";
        public string ApiHost { get => _apiHost; set => SetProperty(ref _apiHost, value); }

        private string _apiPort = "8000";
        public string ApiPort { get => _apiPort; set => SetProperty(ref _apiPort, value); }
    }
}
