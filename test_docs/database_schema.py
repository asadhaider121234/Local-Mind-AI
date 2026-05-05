import sqlite3
import os

# Database Schema Initialization for DocMind
# This script defines the structure of our local metadata storage.
# We use SQLite for lightweight, persistent storage of file-level metadata.

def init_db():
    """Initializes the database and creates necessary tables if they don't exist."""
    db_path = 'docmind.db'
    print(f"Initializing database at: {db_path}")
    
    conn = sqlite3.connect(db_path)
    c = conn.cursor()
    
    # Files table stores path, category, and basic stats
    c.execute('''
        CREATE TABLE IF NOT EXISTS files (
            path TEXT PRIMARY KEY,
            filename TEXT,
            category TEXT,
            size INTEGER,
            modified REAL,
            indexed_at REAL
        )
    ''')
    
    # Chunks table links to files and stores IDs for FAISS mapping
    c.execute('''
        CREATE TABLE IF NOT EXISTS chunks (
            chunk_id INTEGER PRIMARY KEY,
            file_path TEXT,
            text_content TEXT,
            page_number INTEGER,
            FOREIGN KEY(file_path) REFERENCES files(path)
        )
    ''')
    
    conn.commit()
    conn.close()
    print("Database initialization complete.")

if __name__ == "__main__":
    init_db()
    # Ensure the directory exists before proceeding
    if not os.path.exists('data'):
        os.makedirs('data')
