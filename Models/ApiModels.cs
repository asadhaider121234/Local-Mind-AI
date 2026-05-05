using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DocMind.Models
{
    public class HealthResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("llm_loaded")]
        public bool LlmLoaded { get; set; }

        [JsonPropertyName("index_loaded")]
        public bool IndexLoaded { get; set; }

        [JsonPropertyName("total_chunks")]
        public int TotalChunks { get; set; }
    }

    public class QueryRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("file_path")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FilePath { get; set; }
    }

    public class QueryResponse
    {
        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;

        [JsonPropertyName("sources")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<SourceDto> Sources { get; set; } = new();

        [JsonPropertyName("latency_ms")]
        public double LatencyMs { get; set; }
    }

    public class SourceDto
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("excerpt")]
        public string Excerpt { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;
    }

    public class FileDto
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = "Uncategorized";

        [JsonPropertyName("extension")]
        public string Extension { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("modified")]
        public double Modified { get; set; }

        [JsonPropertyName("chunk_count")]
        public int ChunkCount { get; set; }
    }

    public class CategoryOverviewDto
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("files")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<CategoryFileDto> Files { get; set; } = new();
    }

    public class CategoryFileDto
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("extension")]
        public string Extension { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class DuplicateResultDto
    {
        [JsonPropertyName("exact")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DuplicateGroupDto> Exact { get; set; } = new();

        [JsonPropertyName("near")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DuplicateGroupDto> Near { get; set; } = new();
    }

    public class DuplicateGroupDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("similarity")]
        public double Similarity { get; set; }

        [JsonPropertyName("files")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DuplicateFileDto> Files { get; set; } = new();

        [JsonPropertyName("size_kb")]
        public double SizeKb { get; set; }
    }

    public class DuplicateFileDto
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("size_kb")]
        public double SizeKb { get; set; }
    }

    public class DeleteRequest
    {
        [JsonPropertyName("paths")]
        public List<string> Paths { get; set; } = new();
    }

    public class DeleteResponse
    {
        [JsonPropertyName("deleted")]
        public int Deleted { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }
    }

    public class EvaluateRequest
    {
        [JsonPropertyName("query_count")]
        public int QueryCount { get; set; }
    }

    public class EvaluationResponse
    {
        [JsonPropertyName("precision_at_5")]
        public double PrecisionAt5 { get; set; }

        [JsonPropertyName("recall_at_5")]
        public double RecallAt5 { get; set; }

        [JsonPropertyName("mrr")]
        public double Mrr { get; set; }

        [JsonPropertyName("avg_latency_ms")]
        public double AvgLatencyMs { get; set; }

        [JsonPropertyName("embedding_ms")]
        public double EmbeddingMs { get; set; }

        [JsonPropertyName("retrieval_ms")]
        public double RetrievalMs { get; set; }

        [JsonPropertyName("generation_ms")]
        public double GenerationMs { get; set; }

        [JsonPropertyName("queries_run")]
        public int QueriesRun { get; set; }
        
        [JsonPropertyName("semantic_vs_keyword")]
        public EvaluationComparison? SemanticVsKeyword { get; set; }
    }

    public class EvaluationComparison
    {
        [JsonPropertyName("semantic")]
        public EvaluationMetrics? Semantic { get; set; }

        [JsonPropertyName("keyword")]
        public EvaluationMetrics? Keyword { get; set; }
    }

    public class EvaluationMetrics
    {
        [JsonPropertyName("p5")]
        public double P5 { get; set; }

        [JsonPropertyName("r5")]
        public double R5 { get; set; }

        [JsonPropertyName("mrr")]
        public double Mrr { get; set; }
    }
    
    public class IndexingStatusResponse
    {
        [JsonPropertyName("is_indexing")]
        public bool IsIndexing { get; set; }

        [JsonPropertyName("progress")]
        public int Progress { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("current_file")]
        public string CurrentFile { get; set; } = string.Empty;
    }

    public class SummarizeResponse
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
    }
}
