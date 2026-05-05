using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DocMind.Models;
using DocMind.Services;
using Microsoft.Win32;

namespace DocMind.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private AppSettings _currentSettings;
        public AppSettings CurrentSettings
        {
            get => _currentSettings;
            set => SetProperty(ref _currentSettings, value);
        }

        private bool? _isConnected;
        public bool? IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        private bool _showSavedToast;
        public bool ShowSavedToast
        {
            get => _showSavedToast;
            set => SetProperty(ref _showSavedToast, value);
        }

        public ObservableCollection<string> WatchedFolders { get; } = new();

        public ICommand BrowseModelCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand RemoveFolderCommand { get; }
        public ICommand TestConnectionCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ResetDefaultsCommand { get; }

        public SettingsViewModel()
        {
            // Clone settings from service to avoid live edits affecting the app immediately
            var settings = SettingsService.Instance.Settings;
            _currentSettings = new AppSettings
            {
                Theme = settings.Theme,
                LastPage = settings.LastPage,
                AppVersion = settings.AppVersion,
                LlmModelPath = settings.LlmModelPath,
                VisionModel = settings.VisionModel,
                CpuThreads = settings.CpuThreads,
                ContextLength = settings.ContextLength,
                WatchedFolders = new System.Collections.Generic.List<string>(settings.WatchedFolders),
                ChunkSize = settings.ChunkSize,
                ChunkOverlap = settings.ChunkOverlap,
                AutoReindex = settings.AutoReindex,
                IndexPdf = settings.IndexPdf,
                IndexDocx = settings.IndexDocx,
                IndexXlsx = settings.IndexXlsx,
                IndexTxt = settings.IndexTxt,
                IndexCsv = settings.IndexCsv,
                IndexJpg = settings.IndexJpg,
                IndexPng = settings.IndexPng,
                IndexPY = settings.IndexPY,
                IndexCS = settings.IndexCS,
                IndexCPP = settings.IndexCPP,
                IndexJS = settings.IndexJS,
                IndexHTML = settings.IndexHTML,
                IndexCSS = settings.IndexCSS,
                IndexJSON = settings.IndexJSON,
                IndexXML = settings.IndexXML,
                IndexSQL = settings.IndexSQL,
                IndexXAML = settings.IndexXAML,
                IndexIPYNB = settings.IndexIPYNB,
                ResultsPerQuery = settings.ResultsPerQuery,
                MinRelevanceScore = settings.MinRelevanceScore,
                EnableHallucinationFilter = settings.EnableHallucinationFilter,
                EnableImageSearch = settings.EnableImageSearch,
                FontSize = settings.FontSize,
                ShowRelevanceScores = settings.ShowRelevanceScores,
                ShowFilePaths = settings.ShowFilePaths,
                ApiHost = settings.ApiHost,
                ApiPort = settings.ApiPort
            };

            foreach (var f in _currentSettings.WatchedFolders)
                WatchedFolders.Add(f);

            BrowseModelCommand = new RelayCommand(_ => ExecuteBrowseModel());
            AddFolderCommand = new RelayCommand(_ => ExecuteAddFolder());
            RemoveFolderCommand = new RelayCommand(p => ExecuteRemoveFolder(p as string));
            TestConnectionCommand = new RelayCommand(_ => _ = ExecuteTestConnection());
            SaveSettingsCommand = new RelayCommand(_ => ExecuteSave());
            ResetDefaultsCommand = new RelayCommand(_ => ExecuteReset());
        }

        private void ExecuteBrowseModel()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "GGUF Model Files (*.gguf)|*.gguf|All Files (*.*)|*.*",
                Title = "Select LLM Model"
            };
            if (dialog.ShowDialog() == true)
            {
                CurrentSettings.LlmModelPath = dialog.FileName;
                OnPropertyChanged(nameof(CurrentSettings));
            }
        }

        private void ExecuteAddFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Folder to Watch",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                string folder = dialog.FolderName;
                if (!WatchedFolders.Contains(folder))
                {
                    WatchedFolders.Add(folder);
                    CurrentSettings.WatchedFolders.Add(folder);
                }
            }
        }

        private void ExecuteRemoveFolder(string? folder)
        {
            if (folder != null)
            {
                WatchedFolders.Remove(folder);
                CurrentSettings.WatchedFolders.Remove(folder);
            }
        }

        private async Task ExecuteTestConnection()
        {
            IsConnected = null; // Loading state
            
            // Temporarily set global settings to the typed values just for the test
            var global = SettingsService.Instance.Settings;
            string originalHost = global.ApiHost;
            string originalPort = global.ApiPort;
            
            global.ApiHost = CurrentSettings.ApiHost;
            global.ApiPort = CurrentSettings.ApiPort;
            
            try
            {
                var health = await ApiService.Instance.CheckHealthAsync();
                IsConnected = health != null;
                
                if (IsConnected == true)
                {
                    MessageBox.Show($"Connected successfully!\nBackend: {health?.Status}\nLLM Loaded: {health?.LlmLoaded}", 
                                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Could not reach the backend. Please check the Host and Port.", 
                                    "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Revert back
                global.ApiHost = originalHost;
                global.ApiPort = originalPort;
            }
        }

        private void ExecuteSave()
        {
            var global = SettingsService.Instance.Settings;
            
            // Sync current to global
            global.Theme = CurrentSettings.Theme;
            global.LlmModelPath = CurrentSettings.LlmModelPath;
            global.VisionModel = CurrentSettings.VisionModel;
            global.CpuThreads = CurrentSettings.CpuThreads;
            global.ContextLength = CurrentSettings.ContextLength;
            global.WatchedFolders = new System.Collections.Generic.List<string>(WatchedFolders);
            global.ChunkSize = CurrentSettings.ChunkSize;
            global.ChunkOverlap = CurrentSettings.ChunkOverlap;
            global.AutoReindex = CurrentSettings.AutoReindex;
            global.IndexPdf = CurrentSettings.IndexPdf;
            global.IndexDocx = CurrentSettings.IndexDocx;
            global.IndexXlsx = CurrentSettings.IndexXlsx;
            global.IndexTxt = CurrentSettings.IndexTxt;
            global.IndexCsv = CurrentSettings.IndexCsv;
            global.IndexJpg = CurrentSettings.IndexJpg;
            global.IndexPng = CurrentSettings.IndexPng;
            global.IndexPY = CurrentSettings.IndexPY;
            global.IndexCS = CurrentSettings.IndexCS;
            global.IndexCPP = CurrentSettings.IndexCPP;
            global.IndexJS = CurrentSettings.IndexJS;
            global.IndexHTML = CurrentSettings.IndexHTML;
            global.IndexCSS = CurrentSettings.IndexCSS;
            global.IndexJSON = CurrentSettings.IndexJSON;
            global.IndexXML = CurrentSettings.IndexXML;
            global.IndexSQL = CurrentSettings.IndexSQL;
            global.IndexXAML = CurrentSettings.IndexXAML;
            global.IndexIPYNB = CurrentSettings.IndexIPYNB;
            global.ResultsPerQuery = CurrentSettings.ResultsPerQuery;
            global.MinRelevanceScore = CurrentSettings.MinRelevanceScore;
            global.EnableHallucinationFilter = CurrentSettings.EnableHallucinationFilter;
            global.EnableImageSearch = CurrentSettings.EnableImageSearch;
            global.FontSize = CurrentSettings.FontSize;
            global.ShowRelevanceScores = CurrentSettings.ShowRelevanceScores;
            global.ShowFilePaths = CurrentSettings.ShowFilePaths;
            global.ApiHost = CurrentSettings.ApiHost;
            global.ApiPort = CurrentSettings.ApiPort;

            SettingsService.Instance.Save();
            
            // Sync to backend if it's running
            _ = ApiService.Instance.UpdateConfigAsync(global.VisionModel, global.LlmModelPath);
            
            _ = ShowToast();
        }

        private async Task ShowToast()
        {
            ShowSavedToast = true;
            await Task.Delay(3000);
            ShowSavedToast = false;
        }

        private void ExecuteReset()
        {
            if (MessageBox.Show("Reset all settings to defaults?", "Reset", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                CurrentSettings = new AppSettings();
                WatchedFolders.Clear();
                OnPropertyChanged(nameof(CurrentSettings));
            }
        }
    }
}
