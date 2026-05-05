"""
DocMind — Module 5: Document Categorizer
Classifies documents into one of five categories using LLM or keyword fallback.
"""

from typing import Optional

CATEGORIES = ['Financial', 'Academic', 'Personal', 'Legal', 'Technical']

_llm = None

def init_categorizer(llm_instance):
    global _llm
    _llm = llm_instance

def classify_document(text: str) -> str:
    global _llm
    if _llm is not None:
        prompt = (
            "Classify this document into exactly one of these "
            "categories: Financial, Academic, Personal, Legal, "
            "Technical. Reply with only the category name, nothing "
            f"else.\n\nDocument excerpt:\n{text[:500]}\n\nCategory:"
        )
        try:
            response = _llm(
                prompt,
                max_tokens=10,
                temperature=0.1,
                stop=["\n"],
                echo=False
            )
            ans = response["choices"][0]["text"].strip()
            for cat in CATEGORIES:
                if cat.lower() in ans.lower():
                    return cat
        except Exception:
            pass

    # Keyword-based fallback
    lowered = text.lower()
    keywords = {
        'Financial': ['invoice', 'salary', 'tax', 'payment', 'bank', 'receipt', 'bill', 'expense'],
        'Academic': ['assignment', 'lecture', 'course', 'university', 'grade', 'exam', 'thesis'],
        'Legal': ['contract', 'agreement', 'clause', 'law', 'legal', 'court', 'attorney'],
        'Technical': ['code', 'function', 'api', 'server', 'database', 'algorithm', 'software']
    }
    
    best_cat = 'Personal'
    best_score = 0
    for cat, kws in keywords.items():
        score = sum(lowered.count(kw) for kw in kws)
        if score > best_score:
            best_score = score
            best_cat = cat
            
    return best_cat
