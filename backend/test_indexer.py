import os
import sys
from pathlib import Path

# Add the current directory to sys.path so we can import modules
sys.path.append(os.getcwd())

from modules import indexer
from modules import embedder
from modules import categorizer
from modules import rag

# Mock app_state
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

# Initialize dependencies
embedder.load_embedding_model()
# No LLM for this test

test_folder = "C:\\Users\\user\\Desktop\\FYP Project\\test_folder"
print(f"Starting indexing for {test_folder}...")
indexer.run_full_indexing_pipeline(test_folder, app_state, indexing_status)
print("Finished.")
print(f"Total chunks: {app_state['total_chunks']}")
