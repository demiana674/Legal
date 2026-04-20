"""
LegalMate AI Service - Enterprise Edition
AI-Powered Legal Document Analysis with RAG, Smart Search, and Chat
Enhanced with Conversation Memory and PDF Export
"""
from fastapi import FastAPI, UploadFile, File, HTTPException, Depends, Request, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from fastapi.responses import StreamingResponse
from pydantic import BaseModel
import os
import tempfile
import logging
import io
from typing import Optional, List
from datetime import datetime

from model_loader import model_loader
from ai_pipeline import AIPipeline
from smart_search import SmartSearch
from text_extractor import TextExtractor
from conversation_memory import conversation_memory
from utils.sanitizer import sanitizer

# إعداد التسجيل
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[logging.StreamHandler()]
)
logger = logging.getLogger(__name__)

# ==================== Configuration ====================
API_KEY = os.getenv("AI_API_KEY", "legalmate-ai-secret-key-2024")
DATA_PATH = os.getenv("DATA_PATH", "legal_documents.json")
MODEL_TIMEOUT = int(os.getenv("MODEL_TIMEOUT", "60"))

# ==================== FastAPI Setup ====================
app = FastAPI(
    title="LegalMate AI Service",
    version="8.0.0",
    description="AI-Powered Legal Document Analysis Platform with Conversation Memory"
)

# CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Security
security = HTTPBearer(auto_error=False)

def verify_api_key(
    credentials: Optional[HTTPAuthorizationCredentials] = Depends(security),
    request: Request = None
):
    """Verify API key"""
    if not credentials:
        logger.warning("Missing API Key")
        raise HTTPException(status_code=401, detail="Missing API Key")
    
    if credentials.credentials != API_KEY:
        logger.warning("Invalid API Key attempt")
        raise HTTPException(status_code=401, detail="Invalid API Key")
    
    return credentials.credentials

# ==================== Initialize Components ====================
logger.info("Initializing AI Pipeline...")
pipeline = AIPipeline(model_loader)
extractor = TextExtractor()

# Load search index
search_engine = SmartSearch(auto_load_path=DATA_PATH if os.path.exists(DATA_PATH) else None)
if search_engine.is_loaded:
    logger.info(f"Search index loaded with {len(search_engine.documents)} documents")
else:
    logger.warning("Search index not loaded")

# ==================== Request Models ====================
class DocumentRequest(BaseModel):
    text: str

class ChatRequest(BaseModel):
    text: str
    question: str
    user_id: Optional[str] = None
    session_id: Optional[str] = None

class ConversationRequest(BaseModel):
    message: str
    user_id: str
    session_id: Optional[str] = None

class SearchRequest(BaseModel):
    query: str
    limit: int = 5

class ContractRequest(BaseModel):
    template_id: str
    data: dict

class ClearHistoryRequest(BaseModel):
    user_id: str
    session_id: Optional[str] = None

# ==================== Health Check ====================
@app.get("/api/v1/health")
async def health_check(request: Request = None):
    model_info = model_loader.get_model_info()
    memory_stats = conversation_memory.get_stats()
    
    return {
        "status": "healthy",
        "version": "8.0.0",
        "model_loaded": model_loader._generator is not None,
        "model_name": model_info.get("model_name"),
        "device": model_info.get("device"),
        "supports_conversation": model_info.get("supports_conversation", False),
        "search_loaded": search_engine.is_loaded,
        "search_documents": len(search_engine.documents),
        "conversation_memory": memory_stats
    }

# ==================== Document Analysis ====================
@app.post("/api/v1/analyze")
async def analyze_document(
    request: Request,
    doc_request: DocumentRequest,
    api_key: str = Depends(verify_api_key)
):
    """Analyze legal document text"""
    logger.info(f"Analyze request - Text length: {len(doc_request.text)}")
    
    sanitized_text = sanitizer.safe_input(doc_request.text, "text")
    if not sanitized_text or len(sanitized_text.strip()) < 10:
        raise HTTPException(status_code=400, detail="النص المقدم قصير جداً أو غير صالح")
    
    try:
        result = pipeline.analyze_document(sanitized_text)
        return result
    except Exception as e:
        logger.error(f"Analysis error: {e}")
        raise HTTPException(status_code=500, detail=f"خطأ في التحليل: {str(e)}")

@app.post("/api/v1/analyze/file")
async def analyze_file(
    request: Request,
    file: UploadFile = File(...),
    api_key: str = Depends(verify_api_key)
):
    """Analyze uploaded file (PDF, DOCX, TXT)"""
    logger.info(f"File analysis request - Filename: {file.filename}")
    
    try:
        suffix = os.path.splitext(file.filename)[1]
        with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as tmp:
            content = await file.read()
            tmp.write(content)
            tmp_path = tmp.name
        
        text = extractor.extract(tmp_path)
        os.unlink(tmp_path)
        
        if not text or len(text.strip()) < 10:
            raise HTTPException(status_code=400, detail="فشل استخراج النص من الملف")
        
        sanitized_text = sanitizer.safe_input(text, "text")
        
        result = pipeline.analyze_document(sanitized_text)
        result["metadata"]["filename"] = file.filename
        
        return result
        
    except Exception as e:
        logger.error(f"File analysis error: {e}")
        raise HTTPException(status_code=500, detail=f"خطأ في تحليل الملف: {str(e)}")

# ==================== Enhanced Chat with Conversation Memory ====================

@app.post("/api/v1/chat")
async def chat_with_document(
    request: Request,
    chat_request: ChatRequest,
    api_key: str = Depends(verify_api_key)
):
    """Chat with document using RAG (single-turn)"""
    logger.info(f"Chat request - Question: {chat_request.question[:50]}...")
    
    sanitized_text = sanitizer.safe_input(chat_request.text, "text")
    sanitized_question = sanitizer.safe_input(chat_request.question, "question")
    
    if not sanitized_text or not sanitized_question:
        raise HTTPException(status_code=400, detail="إدخال غير صالح")
    
    try:
        result = pipeline.chat_with_doc(sanitized_text, sanitized_question)
        return result
    except Exception as e:
        logger.error(f"Chat error: {e}")
        raise HTTPException(status_code=500, detail=f"خطأ في الإجابة: {str(e)}")

@app.post("/api/v1/conversation")
async def conversation(
    request: Request,
    conv_request: ConversationRequest,
    api_key: str = Depends(verify_api_key)
):
    """
    محادثة متسلسلة مع الذكاء الاصطناعي (مثل ChatGPT)
    يتذكر السياق السابق للمحادثة
    """
    logger.info(f"Conversation request - User: {conv_request.user_id}, Message: {conv_request.message[:50]}...")
    
    # تنظيف المدخلات
    sanitized_message = sanitizer.safe_input(conv_request.message, "question")
    if not sanitized_message:
        raise HTTPException(status_code=400, detail="رسالة غير صالحة")
    
    try:
        # جلب تاريخ المحادثة
        history = conversation_memory.get_history(
            conv_request.user_id, 
            conv_request.session_id
        )
        
        # بناء prompt مع السياق
        if history:
            context_prompt = "المحادثة السابقة:\n"
            for msg in history[-6:]:  # آخر 3 تبادلات
                role_ar = "المستخدم" if msg["role"] == "user" else "المساعد"
                context_prompt += f"{role_ar}: {msg['content']}\n"
            context_prompt += f"\nالمستخدم: {sanitized_message}\nالمساعد:"
        else:
            context_prompt = sanitized_message
        
        # توليد الرد باستخدام النموذج مع تاريخ المحادثة
        if model_loader._generator is not None:
            # تحويل history إلى الصيغة المطلوبة
            formatted_history = []
            for msg in history[-6:]:
                formatted_history.append({
                    "role": msg["role"],
                    "content": msg["content"]
                })
            
            response = model_loader.generate(
                context_prompt,
                max_tokens=1024,
                use_system=True,
                conversation_history=formatted_history
            )
        else:
            response = _fallback_conversation_response(sanitized_message)
        
        # حفظ الرسالة والرد في الذاكرة
        conversation_memory.add_message(
            conv_request.user_id,
            "user",
            sanitized_message,
            conv_request.session_id
        )
        conversation_memory.add_message(
            conv_request.user_id,
            "assistant",
            response,
            conv_request.session_id
        )
        
        logger.info(f"Conversation response generated - Length: {len(response)}")
        
        return {
            "response": response,
            "user_id": conv_request.user_id,
            "session_id": conv_request.session_id,
            "timestamp": datetime.now().isoformat(),
            "history_length": len(history)
        }
        
    except Exception as e:
        logger.error(f"Conversation error: {e}")
        raise HTTPException(status_code=500, detail=f"خطأ في المحادثة: {str(e)}")

@app.post("/api/v1/conversation/history")
async def get_conversation_history(
    request: Request,
    user_id: str,
    session_id: Optional[str] = None,
    api_key: str = Depends(verify_api_key)
):
    """جلب تاريخ المحادثة"""
    history = conversation_memory.get_history(user_id, session_id)
    
    return {
        "user_id": user_id,
        "session_id": session_id,
        "messages": history,
        "message_count": len(history)
    }

@app.post("/api/v1/conversation/clear")
async def clear_conversation_history(
    request: Request,
    clear_request: ClearHistoryRequest,
    api_key: str = Depends(verify_api_key)
):
    """مسح تاريخ المحادثة"""
    conversation_memory.clear_history(clear_request.user_id, clear_request.session_id)
    
    return {
        "status": "success",
        "message": "تم مسح تاريخ المحادثة بنجاح",
        "user_id": clear_request.user_id,
        "session_id": clear_request.session_id
    }

def _fallback_conversation_response(message: str) -> str:
    """رد احتياطي للمحادثة"""
    if "عقد" in message:
        return """يمكنني مساعدتك في فهم العقود القانونية. 

لعقود الإيجار مثلاً، هناك عدة نقاط مهمة يجب الانتباه لها:
• مدة العقد وتاريخ بدايته ونهايته
• قيمة الإيجار وطريقة الدفع
• شروط التجديد والفسخ
• التزامات المؤجر والمستأجر

هل لديك سؤال محدد عن عقد معين؟"""
    
    elif "قضية" in message or "دعوى" in message:
        return """فيما يخص القضايا والدعاوى القضائية، أنصحك بالآتي:

• جمع كل المستندات المتعلقة بالقضية
• استشارة محامٍ متخصص في نوع القضية
• التأكد من مواعيد الجلسات وعدم التخلف عنها
• متابعة إجراءات التقاضي خطوة بخطوة

هل يمكنك توضيح نوع القضية التي تود الاستفسار عنها؟"""
    
    else:
        return """أفهم استفسارك. كمساعد قانوني، يمكنني مساعدتك في:

• فهم المصطلحات القانونية
• شرح أنواع العقود وشروطها
• توضيح الإجراءات القانونية
• تحليل المخاطر في المستندات

هل يمكنك تقديم المزيد من التفاصيل حول ما تبحث عنه؟"""

# ==================== Smart Search ====================
@app.post("/api/v1/search")
async def smart_search(
    request: Request,
    search_request: SearchRequest,
    api_key: str = Depends(verify_api_key)
):
    """Smart semantic search in legal documents"""
    logger.info(f"Search request - Query: {search_request.query[:50]}...")
    
    if not search_engine.is_loaded:
        raise HTTPException(status_code=503, detail="خدمة البحث غير متاحة حالياً")
    
    sanitized_query = sanitizer.safe_input(search_request.query, "query")
    if not sanitized_query:
        raise HTTPException(status_code=400, detail="استعلام بحث غير صالح")
    
    try:
        results = search_engine.search(sanitized_query, search_request.limit)
        return {
            "query": search_request.query,
            "results": results,
            "total": len(results)
        }
    except Exception as e:
        logger.error(f"Search error: {e}")
        raise HTTPException(status_code=500, detail=f"خطأ في البحث: {str(e)}")

# ==================== Quick Analysis ====================
@app.post("/api/v1/analyze/quick")
async def quick_analysis(
    request: Request,
    doc_request: DocumentRequest,
    api_key: str = Depends(verify_api_key)
):
    """Quick risk analysis (summary only)"""
    logger.info(f"Quick analysis request - Text length: {len(doc_request.text)}")
    
    sanitized_text = sanitizer.safe_input(doc_request.text, "text")
    if not sanitized_text or len(sanitized_text.strip()) < 10:
        raise HTTPException(status_code=400, detail="النص المقدم قصير جداً")
    
    try:
        result = pipeline.quick_analysis(sanitized_text)
        return result
    except Exception as e:
        logger.error(f"Quick analysis error: {e}")
        raise HTTPException(status_code=500, detail=f"خطأ في التحليل: {str(e)}")

# ==================== Contract Generation ====================
@app.post("/api/v1/generate-contract")
async def generate_contract(
    request: Request,
    contract_request: ContractRequest,
    api_key: str = Depends(verify_api_key)
):
    """Generate legal contract using AI"""
    logger.info(f"Generate contract request - Template: {contract_request.template_id}")
    
    try:
        templates = {
            "rental": """عقد إيجار

الطرف الأول (المؤجر): {LandlordName}
الطرف الثاني (المستأجر): {TenantName}

العنوان: {PropertyAddress}

المدة: {Duration} سنوات

القيمة الإيجارية الشهرية: {MonthlyRent} جنيه مصري

يبدأ العقد من تاريخ: {StartDate} وينتهي في: {EndDate}

شروط عامة:
1. يلتزم المستأجر بدفع الإيجار في أول كل شهر.
2. يلتزم المؤجر بصيانة العقار.
3. لا يحق للمستأجر التنازل عن العقد أو التأجير من الباطن.

الطرف الأول: __________________
الطرف الثاني: __________________
التاريخ: __________________""",
        
            "employment": """عقد عمل

صاحب العمل: {EmployerName}
الموظف: {EmployeeName}

المسمى الوظيفي: {JobTitle}

الراتب الأساسي: {BaseSalary} جنيه مصري
بدلات: {Allowances} جنيه مصري
إجمالي الراتب: {TotalSalary} جنيه مصري

مدة العقد: {ContractDuration} سنوات
ساعات العمل: {WorkingHours} ساعات يومياً
الإجازات السنوية: {AnnualLeave} يوم

شروط العقد:
1. يلتزم الموظف بأداء مهامه بدقة وأمانة.
2. يلتزم صاحب العمل بدفع الراتب في موعده.
3. تنتهي الخدمة بإخطار كتابي قبل {NoticePeriod} يوم.

صاحب العمل: __________________
الموظف: __________________
التاريخ: __________________""",
        
            "sale": """عقد بيع عقار

البائع: {SellerName}
المشتري: {BuyerName}

العقار المباع: {PropertyDescription}
العنوان: {PropertyAddress}
المساحة: {Area} متر مربع

ثمن البيع: {Price} جنيه مصري
طريقة السداد: {PaymentMethod}
مدة السداد: {PaymentPeriod}
تسليم العقار: {DeliveryDate}

شروط العقد:
1. يقر البائع بأن العقار خالٍ من أي نزاعات قانونية.
2. يتحمل المشتري كافة الرسوم والتكاليف.
3. يتم تسليم العقار فور استلام كامل الثمن.

البائع: __________________
المشتري: __________________
التاريخ: __________________""",
        
            "service": """عقد خدمات استشارية

الجهة المستفيدة: {ClientName}
مقدم الخدمة: {ConsultantName}

نوع الخدمة: {ServiceType}
مدة العقد: {ContractDuration} أشهر
قيمة الخدمة: {ServiceFee} جنيه مصري
مواعيد الدفع: {PaymentSchedule}

التزامات مقدم الخدمة:
1. تقديم الخدمة بجودة عالية.
2. الالتزام بالسرية التامة.
3. تقديم تقارير دورية عن سير العمل.

التزامات المستفيد:
1. توفير المعلومات المطلوبة.
2. الالتزام بدفع المستحقات في موعدها.

مقدم الخدمة: __________________
المستفيد: __________________
التاريخ: __________________""",
        
            "partnership": """عقد شراكة تجارية

الطرف الأول: {Partner1Name}
الطرف الثاني: {Partner2Name}

اسم المشروع: {ProjectName}
نشاط المشروع: {BusinessActivity}
رأس المال: {Capital} جنيه مصري

حصة الطرف الأول: {Partner1Share}% (قيمة: {Partner1Amount} جنيه)
حصة الطرف الثاني: {Partner2Share}% (قيمة: {Partner2Amount} جنيه)

توزيع الأرباح: {ProfitDistribution}
توزيع الخسائر: {LossDistribution}
مدة الشراكة: {PartnershipDuration} سنوات

إدارة المشروع:
يتولى إدارة المشروع: {ManagerName}

شروط عامة:
1. يكون القرار بالإجماع للموافقة.
2. الالتزام بتقديم حسابات دورية.
3. لا يجوز التنازل عن الحصة إلا بموافقة الطرف الآخر.

الطرف الأول: __________________
الطرف الثاني: __________________
التاريخ: __________________"""
        }
        
        template_id_lower = contract_request.template_id.lower()
        template = None
        
        for key in templates:
            if key in template_id_lower:
                template = templates[key]
                break
        
        if not template:
            template = templates["rental"]
            logger.warning(f"Unknown template type: {contract_request.template_id}, using rental template")
        
        contract = template
        for key, value in contract_request.data.items():
            placeholder = "{" + key + "}"
            contract = contract.replace(placeholder, str(value))
        
        if "{Date}" in contract:
            contract = contract.replace("{Date}", datetime.now().strftime("%d/%m/%Y"))
        
        logger.info(f"Contract generated successfully, length: {len(contract)}")
        
        return {
            "content": contract,
            "template_id": contract_request.template_id,
            "generated_at": datetime.now().isoformat()
        }
        
    except Exception as e:
        logger.error(f"Contract generation error: {e}")
        raise HTTPException(status_code=500, detail=f"خطأ في توليد العقد: {str(e)}")

# ==================== Memory Stats ====================
@app.get("/api/v1/memory/stats")
async def get_memory_stats(api_key: str = Depends(verify_api_key)):
    """Get conversation memory statistics"""
    return conversation_memory.get_stats()

# ==================== Root ====================
@app.get("/")
async def root():
    return {
        "message": "LegalMate AI Service - Enhanced Edition",
        "version": "8.0.0",
        "features": [
            "Document Analysis",
            "Chat with Document (RAG)",
            "Conversation Memory (like ChatGPT)",
            "Smart Legal Search",
            "Quick Risk Analysis",
            "Contract Generation"
        ],
        "endpoints": [
            "/api/v1/health",
            "/api/v1/analyze",
            "/api/v1/analyze/file",
            "/api/v1/analyze/quick",
            "/api/v1/chat",
            "/api/v1/conversation",
            "/api/v1/conversation/history",
            "/api/v1/conversation/clear",
            "/api/v1/search",
            "/api/v1/generate-contract",
            "/api/v1/memory/stats"
        ]
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)