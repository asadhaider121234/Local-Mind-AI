using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocMind.Models;

namespace DocMind.Services
{
    public class ApiService
    {
        private static ApiService? _instance;
        public static ApiService Instance => _instance ??= new ApiService();

        // The LLM (Phi-3.5 GGUF on CPU) can take 2-4 minutes to generate an answer.
        // Use a long timeout to avoid false "backend unreachable" errors.
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        private string BaseUrl => $"http://{SettingsService.Instance.Settings.ApiHost}:{SettingsService.Instance.Settings.ApiPort}";

        private ApiService() { }

        public async Task<HealthResponse?> CheckHealthAsync()
        {
            try
            {
                // Health check uses a short dedicated timeout
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _http.GetAsync($"{BaseUrl}/health", cts.Token);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<HealthResponse>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CheckHealthAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<QueryResponse?> QueryAsync(string query, string? filePath = null)
        {
            try
            {
                // LLM can take 2-4 minutes on CPU — use a generous timeout
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(8));
                var request = new QueryRequest { Text = query, FilePath = filePath };
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/query", request, cts.Token);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<QueryResponse>(cancellationToken: cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("QueryAsync timed out after 8 minutes.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QueryAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateConfigAsync(string visionModel, string llmPath)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { vision_model = visionModel, llm_path = llmPath }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _http.PostAsync($"{BaseUrl}/config", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<List<FileDto>?> GetFilesAsync(string? category = null)
        {
            try
            {
                var url = $"{BaseUrl}/files";
                if (!string.IsNullOrEmpty(category))
                {
                    url += $"?category={Uri.EscapeDataString(category)}";
                }
                var response = await _http.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<FileDto>>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetFilesAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<Dictionary<string, CategoryOverviewDto>?> GetCategoriesAsync()
        {
            try
            {
                var response = await _http.GetAsync($"{BaseUrl}/files/categories");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Dictionary<string, CategoryOverviewDto>>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetCategoriesAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<DuplicateResultDto?> GetDuplicatesAsync()
        {
            try
            {
                var response = await _http.GetAsync($"{BaseUrl}/duplicates");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DuplicateResultDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetDuplicatesAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<QueryResponse?> QueryAsync(string text, string? filePath = null, CancellationToken ct = default)
        {
            try
            {
                var request = new QueryRequest { Text = text, FilePath = filePath };
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/query", request, ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<QueryResponse>(cancellationToken: ct);
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception ex)
            {
                Debug.WriteLine($"QueryAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<DeleteResponse?> DeleteFilesAsync(List<string> paths)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/files")
                {
                    Content = JsonContent.Create(new DeleteRequest { Paths = paths })
                };
                var response = await _http.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DeleteResponse>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteFilesAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<EvaluationResponse?> EvaluateAsync(int queryCount, System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/evaluate", new EvaluateRequest { QueryCount = queryCount }, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<EvaluationResponse>(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EvaluateAsync failed: {ex.Message}");
                throw; // let the view model handle cancellations and timeouts
            }
        }
        
        public async Task<HttpResponseMessage?> StartIndexingAsync(string folderPath)
        {
            try
            {
                return await _http.PostAsJsonAsync($"{BaseUrl}/index", new { folder_path = folderPath });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StartIndexingAsync failed: {ex.Message}");
                return null;
            }
        }
        public async Task<IndexingStatusResponse?> GetIndexingStatusAsync()
        {
            try
            {
                var response = await _http.GetAsync($"{BaseUrl}/index/status");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IndexingStatusResponse>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetIndexingStatusAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<SummarizeResponse?> SummarizeAsync(string filePath)
        {
            try
            {
                // LLM summarization can be slow on CPU
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(5));
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/summarize", new { file_path = filePath }, cts.Token);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SummarizeResponse>(cancellationToken: cts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SummarizeAsync failed: {ex.Message}");
                return null;
            }
        }
    }
}
