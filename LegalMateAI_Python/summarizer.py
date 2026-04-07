"""
Summarizer - Generates summaries of legal documents
"""
import re
import logging
from typing import Dict, Any, List

logger = logging.getLogger(__name__)

class Summarizer:
    """
    Summarizes legal documents using LLM or fallback methods
    """
    def __init__(self, model):
        self.model = model
    
    def summarize(self, text: str, max_length: int = 500) -> Dict[str, Any]:
        """
        Generate summary of legal text
        Returns: {
            "summary": str,
            "key_points": List[str],
            "document_type": str,
            "estimated_pages": int
        }
        """
        text_length = len(text)
        estimated_pages = (text_length // 2500) + 1
        
        # Detect document type
        doc_type = self._detect_document_type(text)
        
        # Extract key points
        key_points = self._extract_key_points(text)
        
        # Generate summary using model if available
        if self.model and self.model._generator:
            prompt = f"""
            قم بتلخيص النص القانوني التالي بشكل موجز ومفيد. ركز على النقاط الرئيسية:

            {text[:2000]}

            الملخص:
            """
            summary = self.model.generate(prompt, max_tokens=250)
        else:
            summary = self._fallback_summary(text, key_points)
        
        logger.info(f"Summary generated - Length: {len(summary)}, Type: {doc_type}, Pages: {estimated_pages}")
        
        return {
            "summary": summary[:max_length],
            "key_points": key_points[:5],
            "document_type": doc_type,
            "estimated_pages": estimated_pages,
            "text_length": text_length
        }
    
    def _detect_document_type(self, text: str) -> str:
        """Detect document type from text"""
        text_lower = text.lower()
        
        if "عقد إيجار" in text_lower or "إيجار" in text_lower:
            return "عقد إيجار"
        elif "عقد عمل" in text_lower or "عمل" in text_lower:
            return "عقد عمل"
        elif "عقد بيع" in text_lower or "بيع" in text_lower:
            return "عقد بيع"
        elif "وكالة" in text_lower:
            return "وكالة قانونية"
        elif "قضية" in text_lower or "دعوى" in text_lower:
            return "مستند قضائي"
        else:
            return "مستند قانوني عام"
    
    def _extract_key_points(self, text: str) -> list:
        """Extract key points from text"""
        key_points = []
        sentences = re.split(r'[.。\n]', text)
        
        important_patterns = [
            r"يلتزم|يتعهد",
            r"يحق|له الحق",
            r"غرامة|عقوبة",
            r"مدة|موعد",
            r"قيمة|مبلغ",
        ]
        
        for sent in sentences[:30]:
            for pattern in important_patterns:
                if re.search(pattern, sent, re.IGNORECASE):
                    if len(sent.strip()) > 20:
                        key_points.append(sent.strip()[:150])
                        break
        
        # Remove duplicates while preserving order
        seen = set()
        unique_points = []
        for point in key_points:
            if point not in seen:
                seen.add(point)
                unique_points.append(point)
        
        return unique_points
    
    def _fallback_summary(self, text: str, key_points: list) -> str:
        """Fallback summary when model is unavailable"""
        doc_type = self._detect_document_type(text)
        
        if key_points:
            points_text = " - ".join(key_points[:2])
            return f"هذا {doc_type}. النقاط الرئيسية: {points_text}"
        else:
            preview = text[:200]
            return f"هذا {doc_type}. ملخص: {preview}..."