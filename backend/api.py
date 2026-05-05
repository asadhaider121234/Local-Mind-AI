"""
DocMind Backend — FastAPI Application
Entry point for the offline AI document search assistant.
Run with:  uvicorn api:app --host 127.0.0.1 --port 8000 --reload
"""

import os
import json
import time
import threading
from pathlib import Path
from typing import List, Optional, Dict, Any

from fastapi import FastAPI, HTTPException, Query, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from pydantic import BaseModel

from modules import indexer
from modules import embedder
from modules import rag
from modules import categorizer
from modules import evaluator

# ── Paths ──────────────────────────────────────────────────────────────────────
BASE_DIR       = Path(__file__).parent
MODELS_DIR     = BASE_DIR / "models"
REGISTRY_DIR   = BASE_DIR / "registry"
DATA_DIR       = BASE_DIR / "data"
REGISTRY_FILE  = REGISTRY_DIR / "index_registry.json"
METADATA_FILE  = REGISTRY_DIR / "metadata.json"
FAISS_INDEX    = REGISTRY_DIR / "faiss.index"
FILES_FILE     = REGISTRY_DIR / "files.json"
BENCHMARK_FILE = DATA_DIR    / "benchmark_queries.json"

# ── Shared mutable state ───────────────────────────────────────────────────────
app_state: Dict[str, Any] = {
    "embedder":    None,
    "rag_engine":  None,
    "categorizer": None,
    "metadata":    {"chunks": [], "files": {}},
    "llm_loaded":  False,
    "index_loaded": False,
    "total_chunks": 0,
    "vision_model": "BLIP-2 (Recommended)", # Default
}

indexing_status: Dict[str, Any] = {
    "is_indexing":  False,
    "progress":     0,
    "total":        0,
    "current_file": "",
}

# ── App ────────────────────────────────────────────────────────────────────────
app = FastAPI(
    title="DocMind API",
    description="Offline AI document search assistant — http://127.0.0.1:8000",
    version="1.0.0",
)

@app.exception_handler(RequestValidationError)
async def validation_exception_handler(request, exc):
    print(f"[ValidationError] {exc}")
    return JSONResponse(
        status_code=422,
        content={"detail": exc.errors(), "body": exc.body},
    )

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── Startup ────────────────────────────────────────────────────────────────────

@app.on_event("startup")
async def startup_event():
    _sep = "=" * 60
    print("\n" + _sep + "\n  DocMind Backend Starting Up\n" + _sep)

    print("\n[1/4] Loading BGE embedding model (BAAI/bge-small-en)...")
    try:
        from modules import embedder
        embedder.load_embedding_model()
        app_state["embedder"] = embedder
        print("      [OK] Embedding model loaded")
    except Exception as exc:
        print(f"      [FAIL] Embedding model failed: {exc}")
        app_state["embedder"] = None

    # 2. FAISS index
    print("\n[2/4] Loading FAISS index…")
    if FAISS_INDEX.exists() and app_state["embedder"]:
        try:
            app_state["embedder"].load_faiss_index(str(FAISS_INDEX))
            app_state["index_loaded"] = app_state["embedder"].get_index_stats()["index_loaded"]
            print("      [OK] FAISS index loaded")
        except Exception as exc:
            print(f"      [FAIL] FAISS index load failed: {exc}")
    else:
        print("      [WARN] No FAISS index found -- run POST /index first")

    # 3. Metadata store
    print("\n[3/4] Loading metadata store...")
    try:
        if app_state.get("embedder"):
            app_state["embedder"].load_metadata_store(str(METADATA_FILE))
            stats = app_state["embedder"].get_index_stats()
            app_state["total_chunks"] = stats["total_chunks"]
        else:
            app_state["total_chunks"] = 0
            
        files_data = {}
        if FILES_FILE.exists():
            with open(FILES_FILE, "r", encoding="utf-8") as f:
                files_data = json.load(f)
        app_state["metadata"] = {"files": files_data}
        
        print(f"      [OK] Metadata loaded ({app_state['total_chunks']} chunks, "
              f"{len(files_data)} files)")
    except Exception as exc:
        print(f"      [FAIL] Metadata load failed: {exc}")

    # 4. LLM
    print("\n[4/4] Looking for GGUF model in models/…")
    gguf_files = sorted(MODELS_DIR.glob("*.gguf"))
    rag_engine = rag
    if gguf_files:
        model_path = gguf_files[0]
        print(f"      Found: {model_path.name}")
        try:
            rag_engine.load_llm_model(str(model_path))
            app_state["llm_loaded"] = True
            print("      [OK] LLM loaded")
        except Exception as exc:
            print(f"      [FAIL] LLM load failed: {exc}")
    else:
        print("      [WARN] No .gguf file found -- place Phi-3.5-mini Q4 GGUF in models/")

    app_state["rag_engine"]  = rag_engine
    app_state["categorizer"] = categorizer
    if app_state["llm_loaded"]:
        categorizer.init_categorizer(rag_engine._llm)

    print("\n" + _sep + "\n  DocMind backend ready  ->  http://127.0.0.1:8000\n" + _sep + "\n")


# ── Pydantic models ────────────────────────────────────────────────────────────

class IndexRequest(BaseModel):
    folder_path: str

class QueryRequest(BaseModel):
    text: str
    file_path: Optional[str] = None

class DeleteRequest(BaseModel):
    paths: List[str]

class EvaluateRequest(BaseModel):
    query_count: int = 20

class ConfigUpdateRequest(BaseModel):
    vision_model: Optional[str] = None
    llm_path: Optional[str] = None


# ── Background indexing pipeline ───────────────────────────────────────────────




# ── Endpoints ──────────────────────────────────────────────────────────────────

# 1. Health check
@app.get("/health")
def health_check():
    return {
        "status":       "ok",
        "llm_loaded":   app_state["llm_loaded"],
        "index_loaded": app_state["index_loaded"],
        "total_chunks": app_state["total_chunks"],
        "vision_model": app_state["vision_model"]
    }

# 1.1 Update Config
@app.post("/config")
def update_config(req: ConfigUpdateRequest):
    if req.vision_model is not None:
        app_state["vision_model"] = req.vision_model
        print(f"[Config] Vision model set to: {req.vision_model}")
        # We don't reload vision model immediately, it's loaded on demand in rag.py
        
    if req.llm_path is not None and req.llm_path != "":
        if os.path.exists(req.llm_path):
            try:
                app_state["rag_engine"].load_llm_model(req.llm_path)
                app_state["llm_loaded"] = True
                print(f"[Config] LLM reloaded from: {req.llm_path}")
            except Exception as e:
                print(f"[Config] Failed to reload LLM: {e}")
        else:
            print(f"[Config] LLM path not found: {req.llm_path}")
            
    return {"status": "success", "config": {"vision_model": app_state["vision_model"]}}


# 2. Start indexing
@app.post("/index")
def start_indexing(req: IndexRequest):
    if indexing_status["is_indexing"]:
        raise HTTPException(409, "Indexing already in progress")
    if not os.path.isdir(req.folder_path):
        raise HTTPException(400, f"Folder not found: {req.folder_path}")
    if not app_state["embedder"]:
        raise HTTPException(503, "Embedding model not loaded")

    threading.Thread(
        target=indexer.run_full_indexing_pipeline,
        args=(req.folder_path, app_state, indexing_status),
        daemon=True,
    ).start()

    return {"status": "started", "message": f"Indexing begun for: {req.folder_path}"}


# 3. Indexing status
@app.get("/index/status")
def get_index_status():
    return {
        "is_indexing":  indexing_status["is_indexing"],
        "progress":     indexing_status["progress"],
        "total":        indexing_status["total"],
        "current_file": indexing_status["current_file"],
    }


# 4. Query
@app.post("/query")
def query_documents(req: QueryRequest):
    embedder = app_state.get("embedder")
    if not embedder:
        raise HTTPException(503, "Embedding model not loaded")
    
    # If it's a global query (no file_path), check if index is loaded
    if not req.file_path:
        stats = embedder.get_index_stats()
        if stats["total_chunks"] == 0:
            raise HTTPException(404, "No documents indexed yet — run POST /index first")

    t0     = time.time()
    # Pass file_path to process_query for filtered RAG or VQA
    result = app_state["rag_engine"].process_query(req.text, file_path=req.file_path)
    result["latency_ms"] = int((time.time() - t0) * 1000)
    return result


# 5. List files
@app.get("/files")
def list_files(
    category: Optional[str] = Query(None),
    type:     Optional[str] = Query(None),
    sort:     Optional[str] = Query("name"),
):
    files_data = app_state["metadata"].get("files", {})
    result = []

    for path, info in files_data.items():
        entry = {
            "path":        path,
            "filename":    info.get("filename", os.path.basename(path)),
            "category":    info.get("category",  "Uncategorized"),
            "extension":   info.get("extension",  ""),
            "size":        info.get("size",        0),
            "modified":    info.get("modified",    ""),
            "chunk_count": info.get("chunk_count", 0),
        }
        if category and entry["category"].lower() != category.lower():
            continue
        if type and entry["extension"].lower().lstrip(".") != type.lower():
            continue
        result.append(entry)

    sort_keys = {
        "name":     lambda x: x["filename"].lower(),
        "size":     lambda x: -x["size"],
        "date":     lambda x: str(x["modified"]),
        "category": lambda x: x["category"].lower(),
    }
    result.sort(key=sort_keys.get(sort, sort_keys["name"]))
    return result


# 6. Categories
@app.get("/files/categories")
def get_categories():
    files_data = app_state["metadata"].get("files", {})
    categories: Dict[str, Any] = {}

    for path, info in files_data.items():
        cat = info.get("category", "Uncategorized")
        if cat not in categories:
            categories[cat] = {"count": 0, "files": []}
        categories[cat]["count"] += 1
        categories[cat]["files"].append({
            "path":      path,
            "filename":  info.get("filename",  os.path.basename(path)),
            "extension": info.get("extension", ""),
            "size":      info.get("size",       0),
        })

    return categories


# 7. Summarize
class SummarizeRequest(BaseModel):
    file_path: str

@app.post("/summarize")
def summarize_document(req: SummarizeRequest):
    path = req.file_path
    if not os.path.exists(path):
        raise HTTPException(404, f"File not found: {path}")
    if not app_state.get("embedder"):
        raise HTTPException(503, "Models not loaded")
    try:
        from modules.extractor import dispatch_extractor
        extracted_pages = dispatch_extractor(path)
        text = "\n".join(page["text"] for page in extracted_pages)
        if not text.strip():
            return {"summary": "Could not extract text from this document."}
        
        # Check if LLM is loaded
        if not app_state.get("llm_loaded"):
             return {"summary": "LLM not loaded. Please check settings."}

        summary = app_state["rag_engine"].summarize(text[:8000])
        return {"summary": summary}
    except Exception as exc:
        print(f"[Summarize] Error: {exc}")
        raise HTTPException(500, str(exc))


# 8. Duplicates
@app.get("/duplicates")
def find_duplicates():
    return indexer.detect_all_duplicates(app_state)


# 9. Delete files
@app.delete("/files")
def delete_files(req: DeleteRequest):
    deleted, failed = 0, 0
    metadata = app_state["metadata"]

    for path in req.paths:
        try:
            if os.path.exists(path):
                os.remove(path)
            if path in metadata["files"]:
                del metadata["files"][path]
            # Remove associated chunks
            metadata["chunks"] = [
                c for c in metadata["chunks"] if c.get("source_path") != path
            ]
            deleted += 1
        except Exception as exc:
            print(f"[Delete] Failed {path}: {exc}")
            failed += 1

    if deleted:
        try:
            with open(FILES_FILE, "w", encoding="utf-8") as f:
                json.dump(metadata["files"], f, ensure_ascii=False, default=str, indent=2)
            # Cannot easily remove chunks from FAISS without rebuilding, 
            # so we just keep them or implement rebuilding in a future update.
        except Exception as exc:
            print(f"[Delete] Metadata save failed: {exc}")

    return {"deleted": deleted, "failed": failed}


# 10. Evaluate
@app.post("/evaluate")
def run_evaluation(req: EvaluateRequest):
    if not app_state.get("embedder"):
        raise HTTPException(503, "Embedding model not loaded")

    chunks    = app_state["embedder"].get_all_chunks()
    return evaluator.run_full_evaluation(
        query_count=req.query_count, 
        chunks=chunks, 
        rag_engine=app_state["rag_engine"]
    )


# ── Dev entry point ────────────────────────────────────────────────────────────
if __name__ == "__main__":
    import uvicorn
    uvicorn.run("api:app", host="127.0.0.1", port=8000, reload=False)
