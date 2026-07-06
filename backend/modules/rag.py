"""
DocMind — Module 4: RAG Query Engine
Retrieves relevant chunks from FAISS, builds a prompt,
and calls a local GGUF LLM (Phi-3.5-mini) via llama-cpp-python.
"""

import time
import re
import os
from typing import List, Dict, Any
from modules.embedder import search_index

# GLOBAL STATE
_llm = None
_vqa_model = None
_vqa_processor = None
_current_vqa_type = None

SUMMARIZE_TEMPLATE = """\
<|system|>
You are DocMind, an offline AI document summarizer.
Summarize the following document excerpt clearly and concisely in 3-5 sentences.
<|end|>
<|user|>
{text}
<|end|>
<|assistant|>
"""

def load_llm_model(model_path="models/phi-3.5.gguf"):
    global _llm
    try:
        from llama_cpp import Llama
        _llm = Llama(
            model_path=model_path,
            n_ctx=4096,
            n_gpu_layers=0,
            n_threads=4,
            verbose=False
        )
        print("LLM loaded.")
    except Exception as e:
        print(f"Warning: Failed to load LLM from {model_path}. Error: {e}")
        _llm = None

def build_rag_prompt(query: str, chunks: list, is_filtered: bool = False) -> str:
    prompt = "You are DocMind, an offline document assistant.\n"
    if is_filtered:
        prompt += f"Answer the question using ONLY the provided document below.\n"
    else:
        prompt += "Answer the question using ONLY the sources below.\n"
    
    prompt += "If the answer is not found in the sources, respond\n"
    prompt += "with exactly: 'I could not find this in your documents.'\n"
    prompt += "Be concise. Cite the filename for each fact you state.\n\n"
    prompt += "SOURCES:\n"
    
    for i, chunk in enumerate(chunks, 1):
        filename = chunk.get("filename", "Unknown")
        page = chunk.get("page", 1)
        text = chunk.get("text", "")
        prompt += f"[{i}] {filename}, page {page}:\n{text}\n\n"
        
    prompt += f"QUESTION: {query}\n"
    prompt += "ANSWER:"
    return prompt

def get_vqa_engine(model_type="BLIP-2 (Recommended)"):
    global _vqa_model, _vqa_processor, _current_vqa_type
    
    if model_type == "Disabled":
        return None, None
        
    if _vqa_model is None or _current_vqa_type != model_type:
        try:
            from transformers import BlipProcessor, BlipForQuestionAnswering
            # For simplicity, we use the same base BLIP for both options, 
            # but we could swap for LLaVA if requested.
            print(f"Loading VQA model: {model_type}...")
            _vqa_processor = BlipProcessor.from_pretrained("Salesforce/blip-vqa-base")
            _vqa_model = BlipForQuestionAnswering.from_pretrained("Salesforce/blip-vqa-base")
            _current_vqa_type = model_type
        except Exception as e:
            print(f"Failed to load VQA model: {e}")
            _vqa_model = None
    return _vqa_model, _vqa_processor

def answer_vqa(image_path: str, question: str) -> str:
    # Try to get vision_model setting from api.app_state
    vision_type = "BLIP-2 (Recommended)"
    try:
        from api import app_state
        vision_type = app_state.get("vision_model", vision_type)
    except: pass

    if vision_type == "Disabled":
        return "Vision analysis is disabled in Settings."

    model, processor = get_vqa_engine(vision_type)
    if model is None:
        return "Vision model failed to load. Please check your installation."
    
    try:
        from PIL import Image
        import torch
        image = Image.open(image_path).convert('RGB')
        inputs = processor(image, question, return_tensors="pt")
        out = model.generate(**inputs)
        return processor.decode(out[0], skip_special_tokens=True)
    except Exception as e:
        return f"VQA error: {e}"

def generate_answer(prompt: str) -> str:
    global _llm
    if _llm is None:
        return "LLM not loaded. Please check model path in Settings."
        
    response = _llm(
        prompt,
        max_tokens=512,
        temperature=0.1,
        stop=["QUESTION:", "SOURCES:"]
    )
    return response["choices"][0]["text"].strip()

def hallucination_filter(answer: str, chunks: list) -> bool:
    def get_words(text):
        words = re.findall(r'\b\w{5,}\b', text.lower())
        return set(words)
        
    answer_words = get_words(answer)
    if not answer_words:
        return True # Too short to evaluate
        
    chunk_words = set()
    for chunk in chunks:
        chunk_words.update(get_words(chunk.get("text", "")))
        
    overlap = len(answer_words.intersection(chunk_words))
    ratio = overlap / len(answer_words)
    return ratio > 0.25

def format_citations(chunks: list) -> list[dict]:
    citations = []
    seen = set()
    for chunk in chunks:
        filename = chunk.get("filename", "Unknown")
        page = chunk.get("page", 1)
        key = f"{filename}_{page}"
        if key in seen:
            continue
        seen.add(key)
        
        text = chunk.get("text", "")
        excerpt = text[:150] + ("..." if len(text) > 150 else "")
        citations.append({
            "filename": filename,
            "page": page,
            "excerpt": excerpt,
            "score": float(chunk.get("score", 0.0)),
            "category": chunk.get("category", "Uncategorized")
        })
    return citations

def summarize(text: str) -> str:
    global _llm
    if _llm is None:
        sentences = text.replace("\n", " ").split(". ")
        return ". ".join(sentences[:3]).strip() + "."

    prompt = SUMMARIZE_TEMPLATE.format(text=text[:2000])
    try:
        response = _llm(
            prompt,
            max_tokens=512,
            temperature=0.2,
            stop=["<|end|>", "<|user|>"],
        )
        return response["choices"][0]["text"].strip()
    except Exception as e:
        return f"Summarization failed: {e}"

def process_query(query_text: str, file_path: str = None) -> dict:
    t_total = time.time()

    # Check if we are querying an image specifically for VQA
    if file_path and any(file_path.lower().endswith(ext) for ext in ['.jpg', '.jpeg', '.png']):
        answer = answer_vqa(file_path, query_text)
        latency = int((time.time() - t_total) * 1000)
        return {
            "answer": answer.capitalize() + ".",
            "sources": [{"filename": os.path.basename(file_path), "page": 1, "excerpt": "Visual Analysis", "score": 1.0}],
            "latency_ms": latency,
            "embedding_ms": 0.0,
            "retrieval_ms": 0.0,
            "generation_ms": latency,
        }

    # ── Step 1: Embed query ───────────────────────────────────────────────────
    t0 = time.time()
    from modules.embedder import _embedding_model
    if _embedding_model is not None:
        instruction = "Represent this sentence for searching relevant passages: "
        _embedding_model.encode(
            [instruction + query_text],
            normalize_embeddings=True,
            convert_to_numpy=True,
        )
    embedding_ms = (time.time() - t0) * 1000

    # ── Step 2: FAISS retrieval ───────────────────────────────────────────────
    t1 = time.time()
    chunks = search_index(query_text, k=5)

    # Filter chunks if a specific file_path is requested
    if file_path:
        target_name = os.path.basename(file_path).lower()
        chunks = [c for c in chunks if os.path.basename(c.get("filename", "")).lower() == target_name]
    retrieval_ms = (time.time() - t1) * 1000

    if not chunks:
        total_ms = int((time.time() - t_total) * 1000)
        return {
            "answer": "I could not find relevant information in the selected document." if file_path else "No relevant documents found for your query.",
            "sources": [],
            "latency_ms": total_ms,
            "embedding_ms": round(embedding_ms, 2),
            "retrieval_ms": round(retrieval_ms, 2),
            "generation_ms": 0.0,
        }

    # ── Step 3: LLM generation ────────────────────────────────────────────────
    t2 = time.time()
    prompt = build_rag_prompt(query_text, chunks, is_filtered=(file_path is not None))
    answer = generate_answer(prompt)
    generation_ms = (time.time() - t2) * 1000

    is_grounded = hallucination_filter(answer, chunks)
    if not is_grounded and "I could not find" not in answer:
        answer = "I could not find a reliable answer to this in your documents."

    citations = format_citations(chunks)
    total_ms = int((time.time() - t_total) * 1000)

    return {
        "answer": answer,
        "sources": citations,
        "latency_ms": total_ms,
        "embedding_ms": round(embedding_ms, 2),
        "retrieval_ms": round(retrieval_ms, 2),
        "generation_ms": round(generation_ms, 2),
    }
