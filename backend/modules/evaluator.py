import json
import time
import os
from typing import List, Dict, Any

try:
    from rank_bm25 import BM25Okapi
except ImportError:
    BM25Okapi = None

def load_benchmark_queries(path="data/benchmark_queries.json") -> list:
    """Load and return list of { 'query': str, 'expected_keywords': list } dicts."""
    if not os.path.exists(path):
        return []
    try:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        print(f"Error loading benchmark queries: {e}")
        return []

def precision_at_k(retrieved_files: list, relevant_files: list, k=5) -> float:
    """Return fraction of top-k retrieved that are relevant based on filename."""
    top_k = retrieved_files[:k]
    if not top_k:
        return 0.0
    relevant_count = sum(1 for f in top_k if f in relevant_files)
    return relevant_count / len(top_k)

def recall_at_k(retrieved_files: list, relevant_files: list, k=5) -> float:
    """Return fraction of relevant files that appear in top-k."""
    top_k = retrieved_files[:k]
    if not relevant_files:
        return 0.0
    relevant_found = sum(1 for rel in relevant_files if rel in top_k)
    return relevant_found / len(relevant_files)

def mean_reciprocal_rank(retrieved_files: list, relevant_files: list) -> float:
    """Return 1/rank of first relevant result. 0 if none found."""
    for i, f in enumerate(retrieved_files):
        if f in relevant_files:
            return 1.0 / (i + 1)
    return 0.0

def run_keyword_search(query: str, chunks: list, k=5) -> list:
    """Use rank_bm25.BM25Okapi on all chunk texts. Return top-k chunk filenames."""
    if BM25Okapi is None or not chunks:
        return []
    
    corpus = [chunk.get("text", "").lower().split() for chunk in chunks]
    bm25 = BM25Okapi(corpus)
    tokenized_query = query.lower().split()
    
    top_n = bm25.get_top_n(tokenized_query, chunks, n=k)
    return [chunk.get("filename", "").lower() for chunk in top_n]

def run_full_evaluation(query_count=20, chunks=None, rag_engine=None) -> dict:
    """
    Run query_count queries from benchmark dataset.
    For each query: run semantic search + keyword search.
    Compute P@5, R@5, MRR for both based on source filenames.
    Return full results dict matching POST /evaluate response.
    """
    queries = load_benchmark_queries()[:query_count]
    if not queries or not chunks or not rag_engine:
        return _empty_result()
        
    sem_p5, sem_r5, sem_mrr = [], [], []
    kw_p5, kw_r5, kw_mrr = [], [], []
    latencies = []
    
    for q in queries:
        query_text = q.get("query", "")
        # expected_source is the filename
        expected = [q["expected_source"].lower()] if "expected_source" in q else []
            
        t0 = time.time()
        # Semantic search
        try:
            from modules.embedder import search_index
            sem_chunks = search_index(query_text, k=5)
            sem_retrieved_files = [os.path.basename(c.get("filename", "")).lower() for c in sem_chunks]
        except Exception:
            sem_retrieved_files = []
            
        latencies.append((time.time() - t0) * 1000)
        
        sem_p5.append(precision_at_k(sem_retrieved_files, expected, k=5))
        sem_r5.append(recall_at_k(sem_retrieved_files, expected, k=5))
        sem_mrr.append(mean_reciprocal_rank(sem_retrieved_files, expected))
        
        # Keyword search
        kw_retrieved_files = [os.path.basename(f).lower() for f in run_keyword_search(query_text, chunks, k=5)]
        kw_p5.append(precision_at_k(kw_retrieved_files, expected, k=5))
        kw_r5.append(recall_at_k(kw_retrieved_files, expected, k=5))
        kw_mrr.append(mean_reciprocal_rank(kw_retrieved_files, expected))
        
    def _mean(vals): return sum(vals)/len(vals) if vals else 0.0

    return {
        "precision_at_5":  round(_mean(sem_p5), 4),
        "recall_at_5":     round(_mean(sem_r5), 4),
        "mrr":             round(_mean(sem_mrr), 4),
        "avg_latency_ms":  round(_mean(latencies)),
        "embedding_ms":    0, 
        "retrieval_ms":    0,
        "generation_ms":   0,
        "queries_run":     len(queries),
        "semantic_vs_keyword": {
            "semantic": {
                "p5":  round(_mean(sem_p5), 4),
                "r5":  round(_mean(sem_r5), 4),
                "mrr": round(_mean(sem_mrr), 4),
            },
            "keyword": {
                "p5":  round(_mean(kw_p5), 4),
                "r5":  round(_mean(kw_r5), 4),
                "mrr": round(_mean(kw_mrr), 4),
            },
        },
    }
        
    def _mean(vals): return sum(vals)/len(vals) if vals else 0.0

    return {
        "precision_at_5":  round(_mean(sem_p5), 4),
        "recall_at_5":     round(_mean(sem_r5), 4),
        "mrr":             round(_mean(sem_mrr), 4),
        "avg_latency_ms":  round(_mean(latencies)),
        "embedding_ms":    0, # Stubbed for compatibility
        "retrieval_ms":    0,
        "generation_ms":   0,
        "queries_run":     len(queries),
        "semantic_vs_keyword": {
            "semantic": {
                "p5":  round(_mean(sem_p5), 4),
                "r5":  round(_mean(sem_r5), 4),
                "mrr": round(_mean(sem_mrr), 4),
            },
            "keyword": {
                "p5":  round(_mean(kw_p5), 4),
                "r5":  round(_mean(kw_r5), 4),
                "mrr": round(_mean(kw_mrr), 4),
            },
        },
    }

def _empty_result() -> dict:
    return {
        "precision_at_5":  0.0, "recall_at_5": 0.0, "mrr": 0.0,
        "avg_latency_ms":  0, "embedding_ms": 0, "retrieval_ms": 0, "generation_ms": 0,
        "queries_run":     0,
        "semantic_vs_keyword": {
            "semantic": {"p5": 0.0, "r5": 0.0, "mrr": 0.0},
            "keyword":  {"p5": 0.0, "r5": 0.0, "mrr": 0.0},
        },
    }
