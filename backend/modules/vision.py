"""
DocMind — Module 6: Vision (Stub)
Stub for future image-understanding features.
"""

import os

def load_vision_model():
    print("Vision model not loaded (stub).")
    return None

def generate_image_caption(image_path: str) -> str:
    return f"[Image: {os.path.basename(image_path)}]"

def answer_visual_query(image_path: str, question: str) -> str:
    return "Vision model not yet configured."

def route_visual_query(query_text: str) -> bool:
    VISUAL_KEYWORDS = [
        'image', 'photo', 'picture', 'chart', 'graph', 
        'diagram', 'receipt', 'scan', 'screenshot', 'figure'
    ]
    return any(kw in query_text.lower() for kw in VISUAL_KEYWORDS)
