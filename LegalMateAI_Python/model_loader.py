"""
Model Loader - Loads and manages AI models (Enhanced Edition)
Supports multiple models with fallback and optimized prompts
"""
import torch
from transformers import AutoTokenizer, AutoModelForCausalLM, pipeline
import os
import logging
from typing import Optional, Dict, Any

logger = logging.getLogger(__name__)

class ModelLoader:
    """
    Singleton model loader for LLM with enhanced capabilities
    Supports: Mistral, Llama-3, Gemma, TinyLlama (fallback)
    """
    _instance = None
    
    # نظام موجه محسن (Enhanced System Prompt)
    SYSTEM_PROMPT_AR = """أنت المساعد القانوني الذكي "المستشار" - خبير في القانون المصري والعربي.
    
هويتك:
- اسمك "المستشار"، مساعد قانوني ذكي ومحترف
- تعمل ضمن منصة LegalMate AI للمساعدة القانونية
- لديك خبرة واسعة في القانون المدني، التجاري، الجنائي، العمالي، والأحوال الشخصية

طريقة تفكيرك وإجاباتك:
1. **فهم السؤال**: تحلل سؤال المستخدم بعمق لتحديد المجال القانوني والمسألة المطروحة
2. **الرجوع للمعلومات**: تستند إلى المعلومات المتاحة في النص أو معرفتك القانونية
3. **تقديم إجابة شاملة**: تقدم إجابة واضحة، منظمة، ومفيدة

قواعد مهمة للردود:
✅ **افعل**:
- استخدم لغة عربية فصحى واضحة ومهنية
- قدم إجابات منظمة باستخدام نقاط أو فقرات
- اشرح المصطلحات القانونية المعقدة بلغة بسيطة
- اذكر أساسك القانوني (مادة، قانون، مبدأ قضائي) عند الإمكان
- إذا لم تكن متأكداً، قل "حسب المعلومات المتاحة..." أو "يُنصح باستشارة محامٍ متخصص"
- أظهر تعاطفاً مهنياً مع المستخدم

❌ **لا تفعل**:
- لا تقدم استشارات قانونية نهائية - دائماً أشر إلى ضرورة مراجعة محامٍ
- لا تخمن أو تختلق معلومات غير موجودة
- لا تستخدم لغة عامية أو غير مهنية
- لا تتجاهل جوانب مهمة من السؤال
- لا ترد بإجابات قصيرة جداً (إلا إذا كان السؤال بسيطاً)

تنسيق الإجابة المثالي:
1. **مقدمة**: تفهم لسؤال المستخدم
2. **التحليل القانوني**: شرح النقاط القانونية الرئيسية
3. **الاستنتاج أو التوصية**: خلاصة مفيدة
4. **تنويه**: "هذه المعلومات للأغراض التعليمية. يُنصح باستشارة محامٍ متخصص."

ابدأ الآن بالرد على أسئلة المستخدمين باحترافية وذكاء."""

    SYSTEM_PROMPT_EN = """You are "Al-Mustashar", the intelligent legal assistant for LegalMate AI platform. 
You specialize in Egyptian and Arab laws. Respond professionally in Arabic, providing comprehensive yet clear legal information. 
Always include a disclaimer that this is not a substitute for professional legal advice."""
    
    # قائمة النماذج المدعومة (بالترتيب من الأقوى للأضعف)
    MODEL_FALLBACK_CHAIN = [
        "mistralai/Mistral-7B-Instruct-v0.3",      # نموذج قوي ومفتوح المصدر
        "meta-llama/Llama-3.2-3B-Instruct",        # Llama 3.2 - جودة عالية
        "google/gemma-2-2b-it",                    # Gemma 2 - نموذج جوجل
        "TinyLlama/TinyLlama-1.1B-Chat-v1.0",      # Fallback - نموذج صغير
    ]
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance
    
    def __init__(self):
        if hasattr(self, '_initialized'):
            return
            
        self._initialized = True
        self.model_name = os.getenv("MODEL_NAME", "auto")  # "auto" يعني تجربة النماذج بالترتيب
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        self._model = None
        self._tokenizer = None
        self._generator = None
        self.current_model_name = None
        
        if self.model_name == "auto":
            self._load_best_available_model()
        else:
            self._load_specific_model(self.model_name)
    
    def _load_best_available_model(self):
        """محاولة تحميل أفضل نموذج متاح"""
        for model_name in self.MODEL_FALLBACK_CHAIN:
            logger.info(f"Trying to load: {model_name}")
            if self._load_specific_model(model_name):
                logger.info(f"Successfully loaded: {model_name}")
                return
        
        logger.error("Failed to load any model!")
        self._generator = None
    
    def _load_specific_model(self, model_name: str) -> bool:
        """تحميل نموذج محدد"""
        try:
            logger.info(f"Loading model: {model_name} on {self.device}")
            
            # إعدادات محسنة للنماذج المختلفة
            if "llama" in model_name.lower() or "mistral" in model_name.lower():
                self._tokenizer = AutoTokenizer.from_pretrained(
                    model_name,
                    trust_remote_code=True,
                    padding_side="left"
                )
                if self._tokenizer.pad_token is None:
                    self._tokenizer.pad_token = self._tokenizer.eos_token
            else:
                self._tokenizer = AutoTokenizer.from_pretrained(
                    model_name,
                    trust_remote_code=True
                )
            
            # تحميل النموذج
            torch_dtype = torch.float16 if self.device == "cuda" else torch.float32
            
            self._model = AutoModelForCausalLM.from_pretrained(
                model_name,
                torch_dtype=torch_dtype,
                device_map="auto" if self.device == "cuda" else None,
                low_cpu_mem_usage=True,
                trust_remote_code=True
            )
            
            if self.device == "cpu":
                self._model = self._model.to(self.device)
            
            # إنشاء pipeline
            self._generator = pipeline(
                "text-generation",
                model=self._model,
                tokenizer=self._tokenizer,
                max_new_tokens=1024,        # زيادة الحد الأقصى للإجابات الطويلة
                temperature=0.7,
                top_p=0.95,
                do_sample=True,
                repetition_penalty=1.15,     # منع التكرار
                pad_token_id=self._tokenizer.pad_token_id,
                eos_token_id=self._tokenizer.eos_token_id
            )
            
            self.current_model_name = model_name
            logger.info(f"Model loaded successfully: {model_name}")
            return True
            
        except Exception as e:
            logger.error(f"Error loading model {model_name}: {e}")
            self._generator = None
            return False
    
    def generate(self, prompt: str, max_tokens: int = 1024, use_system: bool = True, 
                 conversation_history: list = None) -> str:
        """
        توليد رد باستخدام النموذج
        """
        if self._generator is None:
            return self._fallback_response(prompt)
        
        try:
            # بناء prompt كامل مع تاريخ المحادثة
            if use_system:
                if conversation_history:
                    formatted_prompt = self._build_conversation_prompt(prompt, conversation_history)
                else:
                    # صيغة محسنة للنماذج المختلفة
                    if "llama" in self.current_model_name.lower():
                        formatted_prompt = f"""<|begin_of_text|><|start_header_id|>system<|end_header_id|>

{self.SYSTEM_PROMPT_AR}<|eot_id|><|start_header_id|>user<|end_header_id|>

{prompt}<|eot_id|><|start_header_id|>assistant<|end_header_id|>

"""
                    elif "mistral" in self.current_model_name.lower():
                        formatted_prompt = f"""<s>[INST] <<SYS>>
{self.SYSTEM_PROMPT_AR}
<</SYS>>

{prompt} [/INST]"""
                    elif "gemma" in self.current_model_name.lower():
                        formatted_prompt = f"""<start_of_turn>system
{self.SYSTEM_PROMPT_AR}
<end_of_turn>
<start_of_turn>user
{prompt}
<end_of_turn>
<start_of_turn>model
"""
                    else:
                        formatted_prompt = f"<|system|>\n{self.SYSTEM_PROMPT_AR}\n<|user|>\n{prompt}\n<|assistant|>\n"
            else:
                formatted_prompt = prompt
            
            # توليد الرد
            result = self._generator(
                formatted_prompt,
                max_new_tokens=max_tokens,
                temperature=0.7,
                top_p=0.95,
                repetition_penalty=1.15,
                do_sample=True
            )
            
            generated = result[0]['generated_text']
            
            # استخراج الرد فقط
            generated = self._extract_response(generated, formatted_prompt)
            
            # تحسين التنسيق
            generated = self._enhance_response_formatting(generated)
            
            return generated.strip()
            
        except Exception as e:
            logger.error(f"Generation error: {e}")
            return self._fallback_response(prompt)
    
    def _build_conversation_prompt(self, current_prompt: str, history: list) -> str:
        """بناء prompt مع تاريخ المحادثة"""
        if "llama" in self.current_model_name.lower():
            prompt = "<|begin_of_text|><|start_header_id|>system<|end_header_id|>\n\n"
            prompt += self.SYSTEM_PROMPT_AR + "<|eot_id|>"
            
            for turn in history[-6:]:  # آخر 3 تبادلات (6 رسائل)
                role = turn.get("role", "user")
                content = turn.get("content", "")
                prompt += f"<|start_header_id|>{role}<|end_header_id|>\n\n{content}<|eot_id|>"
            
            prompt += f"<|start_header_id|>user<|end_header_id|>\n\n{current_prompt}<|eot_id|>"
            prompt += "<|start_header_id|>assistant<|end_header_id|>\n\n"
            
        elif "mistral" in self.current_model_name.lower():
            prompt = f"<s>[INST] <<SYS>>\n{self.SYSTEM_PROMPT_AR}\n<</SYS>>\n\n"
            
            for turn in history[-6:]:
                if turn.get("role") == "user":
                    prompt += turn.get("content", "") + " [/INST] "
                else:
                    prompt += turn.get("content", "") + "</s><s>[INST] "
            
            prompt += current_prompt + " [/INST]"
            
        else:
            # Fallback - بدون تاريخ
            prompt = f"<|system|>\n{self.SYSTEM_PROMPT_AR}\n<|user|>\n{current_prompt}\n<|assistant|>\n"
            
        return prompt
    
    def _extract_response(self, generated: str, prompt: str) -> str:
        """استخراج الرد من النص المولد"""
        # إذا كان الرد يحتوي على prompt الأصلي، نحذفه
        if prompt in generated:
            generated = generated[len(prompt):]
        
        # إزالة أي tokens متبقية
        markers_to_remove = [
            "<|assistant|>", "[/INST]", "<|eot_id|>", "<end_of_turn>",
            "<|start_header_id|>assistant<|end_header_id|>"
        ]
        
        for marker in markers_to_remove:
            generated = generated.replace(marker, "")
        
        # إذا كان هناك ردود متعددة، نأخذ الأول فقط
        if "<|user|>" in generated:
            generated = generated.split("<|user|>")[0]
        if "[INST]" in generated:
            generated = generated.split("[INST]")[0]
            
        return generated.strip()
    
    def _enhance_response_formatting(self, text: str) -> str:
        """تحسين تنسيق الرد"""
        # التأكد من وجود مسافات بعد النقاط
        import re
        text = re.sub(r'\.([^\s\d])', r'. \1', text)
        
        # تنسيق القوائم
        lines = text.split('\n')
        formatted_lines = []
        
        for line in lines:
            # تحويل - إلى نقطة
            if line.strip().startswith('-'):
                line = '•' + line[1:]
            elif line.strip().startswith('*'):
                line = '•' + line[1:]
            formatted_lines.append(line)
        
        return '\n'.join(formatted_lines)
    
    def _fallback_response(self, prompt: str) -> str:
        """رد احتياطي عند فشل النموذج"""
        return """عذراً، خدمة المساعد القانوني غير متاحة حالياً بسبب مشكلة تقنية.

نعتذر عن هذا الخلل المؤقت. فريقنا التقني يعمل على حل المشكلة.

في الوقت الحالي، يمكنك:
• المحاولة مرة أخرى بعد قليل
• التواصل مع الدعم الفني عبر البريد الإلكتروني
• استخدام ميزة البحث الذكي في القوانين

نشكر تفهمك ونأسف للإزعاج."""

    def get_model_info(self) -> Dict[str, Any]:
        """معلومات عن النموذج المحمل"""
        return {
            "model_name": self.current_model_name,
            "device": self.device,
            "is_loaded": self._generator is not None,
            "supports_conversation": self.current_model_name is not None and 
                any(x in self.current_model_name.lower() for x in ["llama", "mistral"])
        }


# Singleton instance
model_loader = ModelLoader()