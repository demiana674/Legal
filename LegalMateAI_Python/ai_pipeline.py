"""
AI Pipeline - Orchestrates all AI services
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
    """
    def __init__(self, model):
        self.risk = RiskAnalyzer()
        self.clause = ClauseExtractor()
        self.summary = Summarizer(model)
        self.chat = ChatWithDocument(model)
        self.metrics = {
            "total_analyses": 0,
            "total_chats": 0,
            "risk_distribution": {"High": 0, "Medium": 0, "Low": 0},
            "avg_risk_score": 0,
            "avg_clauses_count": 0
        }
        self._risk_scores_sum = 0
        self._clauses_sum = 0
    
    def analyze_document(self, text: str) -> Dict[str, Any]:
        """
        Complete document analysis with summary, clauses, and risks
        """
        logger.info(f"Analyzing document - Length: {len(text)}")
        
        # Perform analyses
        summary_result = self.summary.summarize(text)
        clauses_result = self.clause.extract(text)
        risk_result = self.risk.analyze(text)
        
        # Update metrics
        self._update_metrics(risk_result, len(clauses_result))
        
        logger.info(f"Analysis complete - Clauses: {len(clauses_result)}, Risk: {risk_result['risk_level']}, Score: {risk_result['risk_score']}")
        
        return {
            "summary": summary_result,
            "clauses": clauses_result,
            "risk": risk_result,
            "metadata": {
                "text_length": len(text),
                "analysis_timestamp": datetime.now().isoformat()
            }
        }
    
    def chat_with_doc(self, text: str, question: str) -> Dict[str, Any]:
        """
        Chat with document using RAG (Retrieval Augmented Generation)
        """
        logger.info(f"Chat request - Question: {question[:50]}...")
        
        self.metrics["total_chats"] += 1
        result = self.chat.ask(text, question)
        
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
            "risk_level": risk_result["risk_level_ar"],
            "risk_score": risk_result["risk_score"],
            "detected_risks": risk_result["detected_risks"]
        }
        
        logger.info(f"Quick analysis complete - Risk level: {result['risk_level']}, Score: {result['risk_score']}")
        
        return result
    
    def _update_metrics(self, risk_result: Dict, clauses_count: int):
        """Update service metrics"""
        self.metrics["total_analyses"] += 1
        
        level = risk_result["risk_level"]
        self.metrics["risk_distribution"][level] = self.metrics["risk_distribution"].get(level, 0) + 1
        
        # Update averages
        self._risk_scores_sum += risk_result["risk_score"]
        self._clauses_sum += clauses_count
        
        self.metrics["avg_risk_score"] = round(self._risk_scores_sum / self.metrics["total_analyses"], 2)
        self.metrics["avg_clauses_count"] = round(self._clauses_sum / self.metrics["total_analyses"], 2)
    
    def get_metrics(self) -> Dict[str, Any]:
        """Get current service metrics"""
        return {
            **self.metrics,
            "total_requests": self.metrics["total_analyses"] + self.metrics["total_chats"]
        }