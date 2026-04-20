"""
AI Pipeline - Orchestrates all AI services (Enhanced Edition)
"""
import logging
import json
from typing import Dict, Any, List
from datetime import datetime

from risk_analyzer import RiskAnalyzer
from clause_extractor import ClauseExtractor
from summarizer import Summarizer
from chat_with_doc import ChatWithDocument

logger = logging.getLogger(__name__)

class AIPipeline:
    """
    Main AI Pipeline that orchestrates all AI services
    Enhanced with better analysis quality
    """
    def __init__(self, model):
        self.risk = RiskAnalyzer()
        self.clause = ClauseExtractor()
        self.summary = Summarizer(model)
        self.chat = ChatWithDocument(model)
        self.metrics = {
            "total_analyses": 0,
            "total_chats": 0,
            "total_conversations": 0,
            "risk_distribution": {"عالية": 0, "متوسطة": 0, "منخفضة": 0},
            "avg_risk_score": 0,
            "avg_clauses_count": 0
        }
        self._risk_scores_sum = 0
        self._clauses_sum = 0
    
    def analyze_document(self, text: str) -> Dict[str, Any]:
        """
        Complete document analysis with summary, clauses, and risks
        Enhanced with Arabic support
        """
        logger.info(f"Analyzing document - Length: {len(text)}")
        
        # Perform analyses
        summary_result = self.summary.summarize(text)
        clauses_result = self.clause.extract(text)
        risk_result = self.risk.analyze(text)
        
        # Enhanced risk analysis with Arabic descriptions
        risk_result = self._enhance_risk_analysis(risk_result, text)
        
        # Update metrics
        self._update_metrics(risk_result, len(clauses_result))
        
        logger.info(f"Analysis complete - Clauses: {len(clauses_result)}, Risk: {risk_result['risk_level_ar']}, Score: {risk_result['risk_score']}")
        
        return {
            "summary": summary_result,
            "clauses": clauses_result,
            "risk": risk_result,
            "metadata": {
                "text_length": len(text),
                "analysis_timestamp": datetime.now().isoformat(),
                "document_type": summary_result.get("document_type", "مستند قانوني"),
                "estimated_pages": summary_result.get("estimated_pages", 1)
            }
        }
    
    def _enhance_risk_analysis(self, risk_result: Dict, text: str) -> Dict:
        """تحسين تحليل المخاطر مع وصف عربي أفضل"""
        
        # ترجمة مستويات المخاطر
        level_translations = {
            "High": "عالية",
            "Medium": "متوسطة", 
            "Low": "منخفضة",
            "Critical": "حرجة"
        }
        
        risk_result["risk_level_ar"] = level_translations.get(
            risk_result.get("risk_level", "Medium"), 
            "متوسطة"
        )
        
        # إضافة توصيات محسنة
        enhanced_suggestions = []
        for suggestion in risk_result.get("suggestions", []):
            if "غرامة" in suggestion:
                enhanced_suggestions.append("⚠️ انتبه لبنود الغرامات: تأكد من وضوح قيمة الغرامة وشروط تطبيقها.")
            elif "فسخ" in suggestion:
                enhanced_suggestions.append("📋 راجع شروط الفسخ: تأكد من وضوح الحالات التي يحق فيها فسخ العقد.")
            elif "التزام" in suggestion:
                enhanced_suggestions.append("✅ وثق الالتزامات: حدد التزامات كل طرف بشكل واضح ومحدد.")
            else:
                enhanced_suggestions.append(suggestion)
        
        if enhanced_suggestions:
            risk_result["suggestions"] = enhanced_suggestions
        
        return risk_result
    
    def chat_with_doc(self, text: str, question: str) -> Dict[str, Any]:
        """
        Chat with document using RAG (Retrieval Augmented Generation)
        """
        logger.info(f"Chat request - Question: {question[:50]}...")
        
        self.metrics["total_chats"] += 1
        result = self.chat.ask(text, question)
        
        # تحسين الثقة
        confidence_map = {
            "عالية": "عالية (إجابة موثوقة)",
            "متوسطة": "متوسطة (يُنصح بالتحقق)",
            "منخفضة": "منخفضة (المعلومات محدودة)"
        }
        result["confidence_description"] = confidence_map.get(
            result.get("confidence", "متوسطة"), 
            "متوسطة"
        )
        
        logger.info(f"Chat complete - Confidence: {result['confidence']}, Chunks: {result['retrieved_chunks_count']}")
        
        return result
    
    def quick_analysis(self, text: str) -> Dict[str, Any]:
        """
        Quick analysis (summary and risk only)
        """
        logger.info(f"Quick analysis - Length: {len(text)}")
        
        risk_result = self.risk.analyze(text)
        
        result = {
            "risk_summary": self.risk.get_risk_summary(text),
            "clause_summary": self.clause.get_clause_summary(text),
            "risk_level": risk_result.get("risk_level_ar", "متوسطة"),
            "risk_score": risk_result["risk_score"],
            "detected_risks": risk_result["detected_risks"],
            "suggestions": risk_result.get("suggestions", [])[:3]
        }
        
        logger.info(f"Quick analysis complete - Risk level: {result['risk_level']}, Score: {result['risk_score']}")
        
        return result
    
    def _update_metrics(self, risk_result: Dict, clauses_count: int):
        """Update service metrics"""
        self.metrics["total_analyses"] += 1
        
        level = risk_result.get("risk_level_ar", "متوسطة")
        if level in self.metrics["risk_distribution"]:
            self.metrics["risk_distribution"][level] += 1
        
        self._risk_scores_sum += risk_result["risk_score"]
        self._clauses_sum += clauses_count
        
        if self.metrics["total_analyses"] > 0:
            self.metrics["avg_risk_score"] = round(self._risk_scores_sum / self.metrics["total_analyses"], 2)
            self.metrics["avg_clauses_count"] = round(self._clauses_sum / self.metrics["total_analyses"], 2)
    
    def record_conversation(self):
        """تسجيل محادثة في الإحصائيات"""
        self.metrics["total_conversations"] += 1
    
    def get_metrics(self) -> Dict[str, Any]:
        """Get current service metrics"""
        return {
            **self.metrics,
            "total_requests": self.metrics["total_analyses"] + self.metrics["total_chats"] + self.metrics["total_conversations"]
        }