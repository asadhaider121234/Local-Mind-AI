import sys
import os
from pathlib import Path

# Add backend to path
sys.path.append(str(Path(r"c:\Users\user\Desktop\FYP Project\LOCAL MIND AI\backend")))

from modules import indexer
from modules import embedder
from modules import categorizer
from modules import rag

app_state = {
    "embedder": embedder,
    "rag_engine": rag,
    "categorizer": categorizer,
    "metadata": {"chunks": [], "files": {}},
    "llm_loaded": False,
    "index_loaded": False,
    "total_chunks": 0,
}

indexing_status = {
    "is_indexing": False,
    "progress": 0,
    "total": 0,
    "current_file": "",
}

# Mock LLM load for categorizer
class MockLLM:
    def __call__(self, prompt, **kwargs):
        return "Technical"

categorizer.init_categorizer(MockLLM())
embedder.load_embedding_model()

folder_path = r"c:\Users\user\Desktop\FYP Project\LOCAL MIND AI\test_docs"
print(f"Scanning {folder_path}...")
scanned = indexer.scan_directory(folder_path)
print(f"Scanned: {scanned}")

indexer.run_full_indexing_pipeline(folder_path, app_state, indexing_status)

print("\nFinal Metadata Files:")
print(app_state["metadata"]["files"])
