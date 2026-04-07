# LegalMateAI_Python/document_analyzer.py
from utils.text_extractor import TextExtractor
from model_loader import model_loader
import os

class DocumentAnalyzer:
    def __init__(self):
        self.model = model_loader
        self.extractor = TextExtractor()
    
    async def analyze(self, file_path: str, request) -> dict:
        """تحليل المستند"""
        # 1. استخراج النص
        text = self.extractor.extract(file_path)
        
        if not text:
            return {
                "summary": "فشل استخراج النص من المستند",
                "extracted_text": "",
                "result": "فشل في معالجة المستند",
                "clauses": [],
                "risks": []
            }
        
        # 2. توليد الملخص
        summary_prompt = f"""قم بتلخيص النص القانوني التالي بشكل موجز ومفيد:
        
        النص:
        {text[:2000]}
        
        الملخص:"""
        
        summary = self.model.generate(summary_prompt, max_tokens=200)
        
        # 3. استخراج البنود
        clauses = []
        if request and request.extract_clauses:
            clauses_prompt = f"""استخرج البنود الرئيسية من النص القانوني التالي. كل بند في سطر منفصل مع رقمه:
            
            {text[:2000]}
            
            البنود الرئيسية:"""
            
            clauses_text = self.model.generate(clauses_prompt, max_tokens=300)
            
            # تقسيم النص إلى بنود
            lines = clauses_text.split('\n')
            for i, line in enumerate(lines[:5]):
                if line.strip():
                    clauses.append({
                        "title": f"بند {i+1}",
                        "text": line.strip(),
                        "page_number": 1,
                        "interpretation": ""
                    })
        
        # 4. تقييم المخاطر
        risks = []
        if request and request.assess_risks:
            risks_prompt = f"""حدد المخاطر القانونية في النص التالي. لكل مخاطرة: النوع، الوصف، المستوى (عالي/متوسط/منخفض):
            
            {text[:2000]}
            
            المخاطر القانونية:"""
            
            risks_text = self.model.generate(risks_prompt, max_tokens=300)
            
            lines = risks_text.split('\n')
            for i, line in enumerate(lines[:3]):
                if line.strip():
                    risks.append({
                        "type": f"مخاطرة {i+1}",
                        "description": line.strip(),
                        "level": "Medium",
                        "suggestion": ""
                    })
        
        return {
            "summary": summary[:500],
            "extracted_text": text[:500],
            "result": f"تم تحليل المستند بنجاح. تم العثور على {len(clauses)} بند و {len(risks)} مخاطرة.",
            "clauses": clauses,
            "risks": risks
        }