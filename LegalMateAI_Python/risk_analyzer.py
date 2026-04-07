"""
Risk Analyzer - Analyzes legal risks in documents
"""
import re
import logging
from typing import Dict, List, Any

logger = logging.getLogger(__name__)

class RiskAnalyzer:
    """
    Analyzes legal risks in documents based on keywords and patterns
    """
    def __init__(self):
        self.risk_keywords = {
            "غرامة": {"weight": 3, "level": "High", "suggestion": "مراجعة قيمة الغرامة ومدى تناسبها مع المخالفة"},
            "فسخ": {"weight": 4, "level": "High", "suggestion": "تحديد حالات الفسخ بوضوح وحماية حقوق الطرفين"},
            "تأخير": {"weight": 2, "level": "Medium", "suggestion": "إضافة مهلة سماح معقولة قبل تطبيق الغرامة"},
            "تعويض": {"weight": 3, "level": "Medium", "suggestion": "تحديد قيمة التعويض أو آلية حسابها بوضوح"},
            "إلغاء": {"weight": 3, "level": "Medium", "suggestion": "مراجعة شروط الإلغاء وضمان وضوحها"},
            "التزام": {"weight": 2, "level": "Low", "suggestion": "توضيح نطاق الالتزامات لكل طرف"},
            "ضمان": {"weight": 2, "level": "Low", "suggestion": "توضيح نطاق الضمان ومدته"},
            "عقوبة": {"weight": 3, "level": "High", "suggestion": "مراجعة شروط العقوبة والتأكد من قانونيتها"},
            "نزاع": {"weight": 2, "level": "Medium", "suggestion": "تحديد آلية فض النزاع بوضوح"},
        }
        
        self.high_risk_words = ["غرامة", "فسخ", "عقوبة", "إلغاء"]
        self.medium_risk_words = ["تأخير", "تعويض", "نزاع"]
    
    def analyze(self, text: str) -> Dict[str, Any]:
        """
        Analyze risks in legal text
        Returns: {
            "risk_score": int,
            "risk_level": str,
            "risk_level_ar": str,
            "detected_risks": List[str],
            "risk_details": List[Dict],
            "suggestions": List[str]
        }
        """
        text_lower = text.lower()
        score = 0
        detected_risks = []
        risk_details = []
        suggestions = []
        
        for keyword, info in self.risk_keywords.items():
            if keyword in text_lower:
                score += info["weight"]
                detected_risks.append(keyword)
                risk_details.append({
                    "type": keyword,
                    "level": info["level"],
                    "suggestion": info["suggestion"]
                })
                suggestions.append(info["suggestion"])
        
        # Determine risk level
        if score >= 8:
            level = "High"
            level_ar = "عالية"
        elif score >= 4:
            level = "Medium"
            level_ar = "متوسطة"
        else:
            level = "Low"
            level_ar = "منخفضة"
        
        # Add general suggestions
        if "يلتزم" not in text_lower and "التزام" not in text_lower:
            suggestions.append("يُنصح بإضافة بنود واضحة تحدد التزامات كل طرف")
        
        logger.info(f"Risk analysis complete - Score: {score}, Level: {level}, Risks: {len(detected_risks)}")
        
        return {
            "risk_score": score,
            "risk_level": level,
            "risk_level_ar": level_ar,
            "detected_risks": list(set(detected_risks)),
            "risk_details": risk_details,
            "suggestions": list(set(suggestions))[:5]
        }
    
    def get_risk_summary(self, text: str) -> str:
        """Get one-line risk summary"""
        analysis = self.analyze(text)
        if analysis["risk_level"] == "High":
            return f"⚠️ مخاطر عالية: تم رصد {len(analysis['detected_risks'])} عنصر خطر يتطلب مراجعة فورية."
        elif analysis["risk_level"] == "Medium":
            return f"⚠️ مخاطر متوسطة: يُنصح بمراجعة {', '.join(analysis['detected_risks'][:3])}."
        else:
            return "✅ المستند ذو مخاطر منخفضة، يمكن اعتماده بعد مراجعة بسيطة."