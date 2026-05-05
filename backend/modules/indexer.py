"""
DocMind — Module 1: File Indexer
Handles file discovery, SHA-256 hashing, registry management, and duplicate detection.
"""

import os
import json
import hashlib
import time
from pathlib import Path
from typing import List, Dict, Any, Tuple

SUPPORTED_EXTENSIONS = {
    '.pdf', '.docx', '.xlsx', '.txt', '.csv',
    '.ppt', '.pptx', '.odp', '.rtf', '.md', '.log',
    '.jpg', '.jpeg', '.png',
    '.py', '.cs', '.cpp', '.c', '.java', '.js', '.ts',
    '.html', '.css', '.xml', '.json', '.xaml', '.sql',
    '.r', '.ipynb', '.sh', '.bat'
}

SKIP_DIRS = {".git", "__pycache__", "node_modules", "$RECYCLE.BIN", "System Volume Information"}

def scan_directory(root_path: str) -> List[str]:
    """Recursively walk root_path, return all file paths whose extension is in SUPPORTED_EXTENSIONS."""
    found_files = []
    for root, dirs, filenames in os.walk(root_path):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS and not d.startswith(".")]
        for filename in filenames:
            ext = Path(filename).suffix.lower()
            if ext in SUPPORTED_EXTENSIONS:
                found_files.append(os.path.join(root, filename))
    return found_files

def compute_file_hash(file_path: str) -> str:
    """Return SHA-256 hex digest of file contents. Read in 8192-byte chunks."""
    hasher = hashlib.sha256()
    try:
        with open(file_path, "rb") as f:
            for chunk in iter(lambda: f.read(8192), b""):
                hasher.update(chunk)
        return hasher.hexdigest()
    except Exception:
        return ""

def load_index_registry(registry_path: str = "registry/index_registry.json") -> dict:
    """Load registry/index_registry.json. Return {} if file does not exist."""
    if os.path.exists(registry_path):
        try:
            with open(registry_path, "r", encoding="utf-8") as f:
                return json.load(f)
        except Exception:
            pass
    return {}

def save_index_registry(registry: dict, registry_path: str = "registry/index_registry.json"):
    """Save registry to registry/index_registry.json with indent=2."""
    os.makedirs(os.path.dirname(registry_path), exist_ok=True)
    with open(registry_path, "w", encoding="utf-8") as f:
        json.dump(registry, f, ensure_ascii=False, indent=2)

def get_changed_files(scanned: List[str], registry: dict) -> Tuple[List[str], List[str], List[str]]:
    """
    Compare scanned list against registry.
    Return tuple: (new_files, modified_files, deleted_files)
    A file is modified if its hash differs from registry.
    """
    new_files = []
    modified_files = []
    
    reg_files = registry.get("files", {})
    reg_paths = set(reg_files.keys())
    scanned_paths = set(scanned)
    
    deleted_files = list(reg_paths - scanned_paths)
    
    for path in scanned:
        if path not in reg_files:
            new_files.append(path)
        else:
            current_hash = compute_file_hash(path)
            if current_hash != reg_files[path].get("hash"):
                modified_files.append(path)
                
    return new_files, modified_files, deleted_files

def detect_exact_duplicates(file_list: List[str]) -> List[Dict[str, Any]]:
    """Return dict mapping hash -> list of paths for hashes that appear more than once."""
    hash_to_paths: Dict[str, List[str]] = {}
    for path in file_list:
        h = compute_file_hash(path)
        if h:
            hash_to_paths.setdefault(h, []).append(path)
    
    groups = []
    for h, paths in hash_to_paths.items():
        if len(paths) > 1:
            size_kb = 0
            if os.path.exists(paths[0]):
                size_kb = round(os.path.getsize(paths[0]) / 1024, 1)
            groups.append({
                "type": "exact",
                "files": [
                    {
                        "path": p,
                        "filename": os.path.basename(p),
                        "size_kb": size_kb
                    } for p in paths
                ],
                "size_kb": size_kb
            })
    return groups

def detect_semantic_duplicates(app_state: dict, threshold=0.95) -> List[Dict[str, Any]]:
    """
    Cosine similarity on existing FAISS embeddings
    Only runs if FAISS index is loaded
    Returns list of { "type": "near", "similarity": float, "files": [...] }
    """
    embedder = app_state.get("embedder")
    metadata = app_state.get("metadata", {})
    files_data = metadata.get("files", {})
    
    if embedder is None or not app_state.get("index_loaded", False):
        return []
        
    import numpy as np
    from modules.extractor import dispatch_extractor

    paths: List[str] = []
    doc_embeddings: List[Any] = []

    for path in files_data.keys():
        if not os.path.exists(path):
            continue
        try:
            pages = dispatch_extractor(path)
            text = "\n".join(p["text"] for p in pages)
            if not text.strip():
                continue
            snippet = text[:1000]   # embed first 1000 chars for speed
            emb = embedder.embed_texts([snippet])[0]
            paths.append(path)
            doc_embeddings.append(emb)
        except Exception:
            continue

    if len(paths) < 2:
        return []

    matrix = np.array(doc_embeddings, dtype=np.float32)
    norms = np.linalg.norm(matrix, axis=1, keepdims=True)
    norms[norms == 0] = 1
    matrix /= norms

    sim = matrix @ matrix.T

    visited: set = set()
    near_groups: List[Dict[str, Any]] = []

    for i in range(len(paths)):
        if i in visited:
            continue
        group_idxs = [j for j in range(len(paths)) if i != j and sim[i, j] >= threshold]
        if not group_idxs:
            continue
        group_idxs = [i] + group_idxs
        for idx in group_idxs:
            visited.add(idx)
        
        size_kb = round(files_data.get(paths[i], {}).get("size", 0) / 1024, 1)
        near_groups.append({
            "type": "near",
            "similarity": round(float(sim[i, group_idxs[1]]), 4),
            "files": [
                {
                    "path": paths[idx],
                    "filename": os.path.basename(paths[idx]),
                    "size_kb": round(files_data.get(paths[idx], {}).get("size", 0) / 1024, 1)
                }
                for idx in group_idxs
            ],
            "size_kb": size_kb
        })

    return near_groups

def detect_all_duplicates(app_state: dict) -> dict:
    """
    Calls both exact and semantic duplicate functions, combines results.
    """
    metadata = app_state.get("metadata", {})
    file_list = list(metadata.get("files", {}).keys())
    
    exact = detect_exact_duplicates(file_list)
    near = detect_semantic_duplicates(app_state)
            
    return {
        "exact": exact,
        "near": near
    }

def run_full_indexing_pipeline(folder_path: str, app_state: dict, indexing_status: dict):
    from modules.extractor import dispatch_extractor, chunk_text
    
    # Path constants
    base_dir = Path(__file__).parent.parent
    registry_file = str(base_dir / "registry" / "index_registry.json")
    metadata_file = str(base_dir / "registry" / "metadata.json")
    faiss_index_file = str(base_dir / "registry" / "faiss.index")

    indexing_status.update({"is_indexing": True, "progress": 0,
                            "total": 0, "current_file": "Scanning..."})
    try:
        # 1. scan_directory
        scanned_files = scan_directory(folder_path)
        
        # 2. load_index_registry
        registry = load_index_registry(registry_file)
        if "files" not in registry:
            registry["files"] = {}
            
        # 3. get_changed_files
        new_files, modified_files, deleted_files = get_changed_files(scanned_files, registry)
        files_to_process = new_files + modified_files
        
        indexing_status["total"] = len(files_to_process)
        print(f"[Indexer] {len(files_to_process)} new/modified files to process. {len(deleted_files)} deleted.")

        embedder = app_state["embedder"]
        categorizer = app_state["categorizer"]
        metadata = app_state["metadata"]
        
        if "files" not in metadata:
            metadata["files"] = {}

        # 5. Remove deleted files from metadata
        for deleted_path in deleted_files:
            if deleted_path in registry["files"]:
                del registry["files"][deleted_path]
            if deleted_path in metadata["files"]:
                del metadata["files"][deleted_path]

        # 4. Process new and modified files
        new_embeddings = []

        for i, file_path in enumerate(files_to_process):
            indexing_status.update({"progress": i + 1, "current_file": file_path})
            filename = os.path.basename(file_path)
            
            try:
                # Calculate file stats
                stat = os.stat(file_path)
                file_size = stat.st_size
                file_mtime = stat.st_mtime
                file_hash = compute_file_hash(file_path)
                ext = Path(file_path).suffix.lower()

                # a. dispatch_extractor
                extracted_pages = dispatch_extractor(file_path)
                if not extracted_pages:
                    continue
                
                full_text = "\n".join(page["text"] for page in extracted_pages)
                if not full_text.strip():
                    continue

                # b. classify_document -> category
                category = categorizer.classify_document(full_text)
                
                chunks = []
                for page_dict in extracted_pages:
                    metadata_dict = {
                        "filename": filename,
                        "page": page_dict["page"],
                        "file_type": ext
                    }
                    page_chunks = chunk_text(page_dict["text"], metadata=metadata_dict)
                    chunks.extend(page_chunks)
                    
                if not chunks:
                    continue

                # c. generate_embeddings_batch -> vectors
                for chunk in chunks:
                    chunk["category"] = category
                    
                vecs = embedder.generate_embeddings_batch(chunks)

                # d/e. Add to metadata store / prepare for FAISS
                embedder.add_chunks_to_index(chunks, vecs)

                metadata["files"][file_path] = {
                    "hash": file_hash,
                    "size": file_size,
                    "modified": file_mtime,
                    "category": category,
                    "chunk_count": len(chunks),
                    "filename": filename,
                    "extension": ext,
                }
                
                # f. update registry with new hash
                registry["files"][file_path] = {
                    "hash": file_hash,
                    "size": file_size,
                    "modified": file_mtime,
                    "indexed_at": time.time(),
                }
                print(f"[Indexer] ({i+1}/{len(files_to_process)}) {filename}")

            except Exception as exc:
                print(f"[Indexer] Skipped {file_path}: {exc}")

        # 6. save_index_registry
        save_index_registry(registry, registry_file)
        
        # 7. Add to FAISS index and save
        embedder.save_faiss_index(faiss_index_file)
            
        # 8. save_metadata_store
        embedder.save_metadata_store(metadata_file)
        
        # Save files metadata separately
        files_file = str(base_dir / "registry" / "files.json")
        with open(files_file, "w", encoding="utf-8") as f:
            json.dump(metadata["files"], f, ensure_ascii=False, default=str, indent=2)

        app_state["metadata"] = metadata
        app_state["total_chunks"] = embedder.get_index_stats()["total_chunks"]
        app_state["index_loaded"] = True
        print(f"[Indexer] Done -- Index updated.")

    except Exception as exc:
        import traceback
        print(f"[Indexer] Pipeline error: {exc}")
        traceback.print_exc()
    finally:
        indexing_status.update({"is_indexing": False, "current_file": "Done"})
