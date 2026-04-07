"""
Input Sanitizer - Protects against prompt injection
"""
import re
import logging

logger = logging.getLogger(__name__)

class InputSanitizer:
    """
    Sanitizes user input to prevent prompt injection
    """
    
    SUSPICIOUS_PATTERNS = [
        r"ignore previous instructions",
        r"ignore all previous",
        r"you are now",
        r"you are a",
        r"act as if",
        r"pretend you are",
        r"system:",
        r"<\|system\|>",
        r"<\|user\|>",
        r"<\|assistant\|>",
        r"forget everything",
        r"disregard",
        r"override",
        r"bypass",
    ]
    
    MAX_TEXT_LENGTH = 50000
    MAX_QUESTION_LENGTH = 1000
    MAX_QUERY_LENGTH = 200
    
    @classmethod
    def sanitize_text(cls, text: str) -> str:
        """Sanitize text input"""
        if not text:
            return ""
        
        if len(text) > cls.MAX_TEXT_LENGTH:
            text = text[:cls.MAX_TEXT_LENGTH]
            logger.warning(f"Text truncated to {cls.MAX_TEXT_LENGTH} chars")
        
        # Remove control characters
        text = re.sub(r'[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]', '', text)
        
        # Normalize whitespace
        text = re.sub(r'\s+', ' ', text)
        
        return text.strip()
    
    @classmethod
    def sanitize_question(cls, question: str) -> str:
        """Sanitize question input"""
        if not question:
            return ""
        
        if len(question) > cls.MAX_QUESTION_LENGTH:
            question = question[:cls.MAX_QUESTION_LENGTH]
            logger.warning(f"Question truncated to {cls.MAX_QUESTION_LENGTH} chars")
        
        return question.strip()
    
    @classmethod
    def sanitize_query(cls, query: str) -> str:
        """Sanitize search query"""
        if not query:
            return ""
        
        if len(query) > cls.MAX_QUERY_LENGTH:
            query = query[:cls.MAX_QUERY_LENGTH]
            logger.warning(f"Query truncated to {cls.MAX_QUERY_LENGTH} chars")
        
        return query.strip()
    
    @classmethod
    def detect_prompt_injection(cls, text: str) -> bool:
        """Detect potential prompt injection"""
        text_lower = text.lower()
        
        for pattern in cls.SUSPICIOUS_PATTERNS:
            if re.search(pattern, text_lower, re.IGNORECASE):
                logger.warning(f"Potential prompt injection detected: {pattern}")
                return True
        
        return False
    
    @classmethod
    def safe_input(cls, text: str, input_type: str = "text") -> str:
        """Complete input validation pipeline"""
        # Sanitize
        if input_type == "question":
            sanitized = cls.sanitize_question(text)
        elif input_type == "query":
            sanitized = cls.sanitize_query(text)
        else:
            sanitized = cls.sanitize_text(text)
        
        # Check for prompt injection
        if cls.detect_prompt_injection(sanitized):
            logger.warning(f"Prompt injection blocked in {input_type}")
            return ""
        
        return sanitized


sanitizer = InputSanitizer()