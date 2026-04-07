"""
Clause Extractor - Extracts important clauses from legal documents
"""
import re
import logging
from typing import List, Dict, Any

logger = logging.getLogger(__name__)

class ClauseExtractor:
    """
    Extracts and ranks important clauses from legal text
    """
    def __init__(self):
        self.important_patterns = [
            r"(يلتزم|يتعهد|يضمن)",
            r"(يحق|له الحق|يستحق)",
            r"(غرامة|عقوبة|جزاء)",
            r"(فسخ|إنهاء|إلغاء)",
            r"(تعويض|تضمين)",
            r"(مدة|موعد|مهلة)",
            r"(ضمان|كفالة)",
        ]
    
    def extract(self, text: str, max_clauses: int = 10) -> List[Dict[str, Any]]:
        """
        Extract important clauses from text
        Returns: List of {"title": str, "text": str, "importance": str, "keywords": List[str]}
        """
        clauses = []
        
        # Try numbered clauses
        numbered_clauses = re.split(r'(?:\n|^)(\d+[\.\-–]\s*)', text)
        
        for i in range(1, len(numbered_clauses), 2):
            if i + 1 < len(numbered_clauses):
                clause_num = numbered_clauses[i].strip()
                clause_text = numbered_clauses[i + 1].strip()
                if clause_text and len(clause_text) > 10:
                    importance, keywords = self._assess_importance(clause_text)
                    clauses.append({
                        "title": f"بند {clause_num}",
                        "text": clause_text[:500],
                        "importance": importance,
                        "keywords": keywords
                    })
        
        # If no numbered clauses, split by paragraphs
        if len(clauses) == 0:
            paragraphs = text.split('\n\n')
            for i, para in enumerate(paragraphs):
                if para.strip() and len(para.strip()) > 20:
                    importance, keywords = self._assess_importance(para)
                    clauses.append({
                        "title": f"فقرة {i + 1}",
                        "text": para[:500],
                        "importance": importance,
                        "keywords": keywords
                    })
        
        # Filter and sort by importance
        clauses = [c for c in clauses if c["importance"] != "low" or len(clauses) < 10]
        clauses = clauses[:max_clauses]
        
        importance_order = {"critical": 0, "high": 1, "medium": 2, "low": 3}
        clauses.sort(key=lambda x: importance_order.get(x["importance"], 3))
        
        logger.info(f"Extracted {len(clauses)} clauses")
        return clauses
    
    def _assess_importance(self, text: str) -> tuple:
        """Assess clause importance based on keywords"""
        text_lower = text.lower()
        keywords = []
        importance = "low"
        
        for pattern in self.important_patterns:
            if re.search(pattern, text_lower):
                keywords.append(re.search(pattern, text_lower).group())
        
        if any(word in text_lower for word in ["غرامة", "فسخ", "عقوبة"]):
            importance = "critical"
        elif any(word in text_lower for word in ["يلتزم", "يحق", "يضمن"]):
            importance = "high"
        elif any(word in text_lower for word in ["مدة", "موعد"]):
            importance = "medium"
        
        return importance, list(set(keywords))[:5]
    
    def get_clause_summary(self, text: str) -> str:
        """Get summary of extracted clauses"""
        clauses = self.extract(text)
        if not clauses:
            return "لم يتم العثور على بنود واضحة في المستند."
        
        critical = [c for c in clauses if c["importance"] == "critical"]
        high = [c for c in clauses if c["importance"] == "high"]
        
        summary = f"تم استخراج {len(clauses)} بنداً مهماً. "
        if critical:
            summary += f"⚠️ {len(critical)} بنداً ذو أهمية قصوى. "
        if high:
            summary += f"📌 {len(high)} بنداً يتضمن التزامات جوهرية."
        
        return summary