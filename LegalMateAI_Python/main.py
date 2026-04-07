"""
LegalMate AI Service - Enterprise Edition
AI-Powered Legal Document Analysis with RAG, Smart Search, and Chat
"""
from fastapi import FastAPI, UploadFile, File, HTTPException, Depends, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from pydantic import BaseModel
import os
import tempfile
import logging
from typing import Optional
from datetime import datetime

from model_loader import model_loader
from ai_pipeline import AIPipeline
from smart_search import SmartSearch
from text_extractor import TextExtractor
from utils.sanitizer import sanitizer

# ✅ تبسيط الـ logging - إزالة trace_id من التنسيق
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
    version="7.0.0",
    description="AI-Powered Legal Document Analysis Platform"
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

class SearchRequest(BaseModel):
    query: str
    limit: int = 5

# ==================== Contract Generation Models ====================
class ContractRequest(BaseModel):
    template_id: str
    data: dict

# ==================== Health Check ====================
@app.get("/api/v1/health")
async def health_check(request: Request = None):
    return {
        "status": "healthy",
        "version": "7.0.0",
        "model_loaded": model_loader._generator is not None,
        "search_loaded": search_engine.is_loaded,
        "search_documents": len(search_engine.documents)
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
    
    # Input sanitization
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
        # Save temp file
        suffix = os.path.splitext(file.filename)[1]
        with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as tmp:
            content = await file.read()
            tmp.write(content)
            tmp_path = tmp.name
        
        # Extract text
        text = extractor.extract(tmp_path)
        os.unlink(tmp_path)
        
        if not text or len(text.strip()) < 10:
            raise HTTPException(status_code=400, detail="فشل استخراج النص من الملف")
        
        # Sanitize
        sanitized_text = sanitizer.safe_input(text, "text")
        
        result = pipeline.analyze_document(sanitized_text)
        result["metadata"]["filename"] = file.filename
        
        return result
        
    except Exception as e:
        logger.error(f"File analysis error: {e}")
        raise HTTPException(status_code=500, detail=f"خطأ في تحليل الملف: {str(e)}")

# ==================== Chat with Document ====================
@app.post("/api/v1/chat")
async def chat_with_document(
    request: Request,
    chat_request: ChatRequest,
    api_key: str = Depends(verify_api_key)
):
    """Chat with document using RAG"""
    logger.info(f"Chat request - Question: {chat_request.question[:50]}...")
    
    # Input sanitization
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
    
    # Input sanitization
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
        # قوالب العقود الأساسية
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
        
        # تحديد نوع القالب
        template_id_lower = contract_request.template_id.lower()
        template = None
        
        for key in templates:
            if key in template_id_lower:
                template = templates[key]
                break
        
        if not template:
            template = templates["rental"]
            logger.warning(f"Unknown template type: {contract_request.template_id}, using rental template")
        
        # ملء البيانات في القالب
        contract = template
        for key, value in contract_request.data.items():
            placeholder = "{" + key + "}"
            contract = contract.replace(placeholder, str(value))
        
        # إضافة تاريخ اليوم إذا لم يكن موجوداً
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

# ==================== Root ====================
@app.get("/")
async def root():
    return {
        "message": "LegalMate AI Service",
        "version": "7.0.0",
        "endpoints": ["/api/v1/health", "/api/v1/analyze", "/api/v1/chat", "/api/v1/search", "/api/v1/generate-contract"]
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)