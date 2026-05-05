"""
DocMind — Module 3: Embedding Engine
Manages BGE model, FAISS index, and chunk metadata via global singletons.
"""

import os
import json
import numpy as np
import faiss

# GLOBAL STATE (module-level singletons)
_embedding_model = None
_faiss_index = None
_metadata_store = {}   # chunk_id (str) → chunk dict
_next_id = 0

def load_embedding_model():
    global _embedding_model
    from sentence_transformers import SentenceTransformer
    _embedding_model = SentenceTransformer('BAAI/bge-small-en')
    print("BGE model loaded.")

def generate_embeddings_batch(chunks: list, batch_size=64) -> np.ndarray:
    if _embedding_model is None:
        raise RuntimeError("Embedding model not loaded.")
        
    texts = [c.get("text", "") for c in chunks]
    embeddings = _embedding_model.encode(
        texts,
        batch_size=batch_size,
        normalize_embeddings=True,
        convert_to_numpy=True
    )
    return embeddings.astype(np.float32)

def build_faiss_index(embeddings: np.ndarray):
    global _faiss_index
    dim = 384
    # IndexFlatIP is simpler and doesn't require training, 
    # perfect for local document search where scale is in thousands, not millions.
    _faiss_index = faiss.IndexFlatIP(dim)
    _faiss_index.add(embeddings)

def add_chunks_to_index(chunks: list, embeddings: np.ndarray):
    global _next_id, _faiss_index
    if _faiss_index is None:
        build_faiss_index(embeddings)
    else:
        _faiss_index.add(embeddings)
        
    for chunk in chunks:
        chunk["chunk_id"] = _next_id
        _metadata_store[str(_next_id)] = chunk
        _next_id += 1

def search_index(query_text: str, k=5) -> list:
    if _embedding_model is None or _faiss_index is None:
        return []
        
    # Embed query with instruction for BGE models
    instruction = "Represent this sentence for searching relevant passages: "
    query_vec = _embedding_model.encode(
        [instruction + query_text],
        normalize_embeddings=True,
        convert_to_numpy=True
    ).astype(np.float32)
    
    # IndexFlatIP does not use nprobe
    
    # query_vec must be 2D array
    q = query_vec.reshape(1, -1)
    scores, indices = _faiss_index.search(q, k)
    
    results = []
    for score, idx in zip(scores[0], indices[0]):
        if idx != -1:
            chunk_data = _metadata_store.get(str(idx))
            if chunk_data:
                # create copy so we don't modify store
                res = dict(chunk_data)
                res["score"] = float(score)
                results.append(res)
    return results

def save_faiss_index(path="registry/faiss.index"):
    if _faiss_index is not None:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        faiss.write_index(_faiss_index, path)

def load_faiss_index(path="registry/faiss.index"):
    global _faiss_index
    if os.path.exists(path):
        _faiss_index = faiss.read_index(path)

def save_metadata_store(path="registry/metadata.json"):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(_metadata_store, f, indent=2, ensure_ascii=False)

def load_metadata_store(path="registry/metadata.json"):
    global _metadata_store, _next_id
    if os.path.exists(path):
        with open(path, 'r', encoding='utf-8') as f:
            data = json.load(f)
            _metadata_store = data
            if data:
                _next_id = max(int(k) for k in data.keys()) + 1
            else:
                _next_id = 0

def get_all_chunks() -> list:
    return list(_metadata_store.values())

def get_index_stats() -> dict:
    return {
        "total_chunks": _next_id,
        "index_loaded": _faiss_index is not None
    }
