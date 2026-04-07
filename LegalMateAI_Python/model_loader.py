"""
Model Loader - Loads and manages AI models
"""
import torch
from transformers import AutoTokenizer, AutoModelForCausalLM, pipeline
import os
import logging

logger = logging.getLogger(__name__)

class ModelLoader:
    """
    Singleton model loader for LLM
    """
    _instance = None
    
    # System prompt for legal assistance
    SYSTEM_PROMPT = """أنت مساعد قانوني مصري متخصص. لديك خبرة في القانون المدني، التجاري، والعقاري.
تعليمات مهمة:
1. أجب فقط بناءً على المعلومات المتاحة
2. إذا لم تكن متأكداً، قل "لا توجد معلومات كافية"
3. استخدم لغة عربية فصحى واضحة
4. لا تقدم استشارات قانونية نهائية - دائماً أشر إلى ضرورة استشارة محامٍ"""
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance
    
    def __init__(self):
        self.model_name = os.getenv("MODEL_NAME", "TinyLlama/TinyLlama-1.1B-Chat-v1.0")
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        self._model = None
        self._tokenizer = None
        self._generator = None
        self._load_model()
    
    def _load_model(self):
        """Load model with fallback"""
        try:
            logger.info(f"Loading model: {self.model_name} on {self.device}")
            
            self._tokenizer = AutoTokenizer.from_pretrained(
                self.model_name,
                trust_remote_code=True
            )
            
            self._model = AutoModelForCausalLM.from_pretrained(
                self.model_name,
                torch_dtype=torch.float16 if self.device == "cuda" else torch.float32,
                device_map="auto" if self.device == "cuda" else None,
                low_cpu_mem_usage=True
            )
            
            if self.device == "cpu":
                self._model = self._model.to(self.device)
            
            self._generator = pipeline(
                "text-generation",
                model=self._model,
                tokenizer=self._tokenizer,
                max_new_tokens=512,
                temperature=0.7,
                top_p=0.95,
                do_sample=True,
                repetition_penalty=1.1
            )
            
            logger.info("Model loaded successfully!")
            
        except Exception as e:
            logger.error(f"Error loading model: {e}")
            self._generator = None
    
    def generate(self, prompt: str, max_tokens: int = 512, use_system: bool = True) -> str:
        """Generate text using model"""
        if self._generator is None:
            return self._fallback_response(prompt)
        
        try:
            # Add system prompt
            if use_system:
                formatted_prompt = f"<|system|>\n{self.SYSTEM_PROMPT}\n<|user|>\n{prompt}\n<|assistant|>\n"
            else:
                formatted_prompt = prompt
            
            result = self._generator(
                formatted_prompt,
                max_new_tokens=max_tokens,
                temperature=0.7,
                top_p=0.95
            )
            
            generated = result[0]['generated_text']
            
            # Extract only the response
            if "<|assistant|>" in generated:
                generated = generated.split("<|assistant|>")[-1]
            
            return generated.strip()
            
        except Exception as e:
            logger.error(f"Generation error: {e}")
            return self._fallback_response(prompt)
    
    def _fallback_response(self, prompt: str) -> str:
        """Fallback response when model fails"""
        return "نعتذر، خدمة الذكاء الاصطناعي غير متاحة حالياً. يرجى المحاولة مرة أخرى لاحقاً."


# Singleton instance
model_loader = ModelLoader()