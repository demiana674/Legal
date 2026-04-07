using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    /// <summary>
    /// واجهة خدمة الذكاء الاصطناعي
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// تحليل مستند قانوني باستخدام الذكاء الاصطناعي
        /// </summary>
        Task<AIAnalysisResult> AnalyzeDocumentAsync(Document document, byte[] fileContent);
        
        /// <summary>
        /// الدردشة مع المستند (RAG)
        /// </summary>
        Task<ChatWithDocumentResponse> ChatWithDocumentAsync(string text, string question);
        
        /// <summary>
        /// بحث ذكي في المستندات القانونية
        /// </summary>
        Task<List<DTOs.ReadDTO.SearchResultDto>> SmartSearchAsync(string query, int limit = 10);
        
        /// <summary>
        /// تحليل سريع (ملخص + مخاطر فقط)
        /// </summary>
        Task<QuickAnalysisResult> QuickAnalysisAsync(string text);
        
        /// <summary>
        /// إنشاء عقد باستخدام AI
        /// </summary>
        Task<string> GenerateContractAsync(ContractTemplate template, Dictionary<string, string> data);
        
        /// <summary>
        /// اقتراح محامين مناسبين بناءً على محتوى المستند
        /// </summary>
        Task<List<DTOs.ReadDTO.LawyerSuggestionDto>> SuggestLawyersAsync(string documentContent, string specialization);
        
        /// <summary>
        /// التحقق من صحة الخدمة
        /// </summary>
        Task<bool> HealthCheckAsync();
    }
    
    /// <summary>
    /// نتيجة تحليل المستند
    /// </summary>
    public class AIAnalysisResult
    {
        public string Summary { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public List<AIClause> Clauses { get; set; } = new();
        public List<AIRisk> Risks { get; set; } = new();
        public List<DTOs.ReadDTO.LawyerSuggestionDto> SuggestedLawyers { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public bool IsFallback { get; set; }
    }
    
    /// <summary>
    /// بند في المستند
    /// </summary>
    public class AIClause
    {
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string Interpretation { get; set; } = string.Empty;
        public string Importance { get; set; } = "Medium";
    }
    
    /// <summary>
    /// خطر قانوني
    /// </summary>
    public class AIRisk
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RiskLevel Level { get; set; }
        public string Suggestion { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// رد الدردشة مع المستند
    /// </summary>
    public class ChatWithDocumentResponse
    {
        public string Answer { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
        public string RelevantContext { get; set; } = string.Empty;
        public int RetrievedChunksCount { get; set; }
        public double TopScore { get; set; }
        public bool IsFallback { get; set; }
    }
    
    /// <summary>
    /// نتيجة التحليل السريع
    /// </summary>
    public class QuickAnalysisResult
    {
        public string RiskSummary { get; set; } = string.Empty;
        public string ClauseSummary { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public List<string> DetectedRisks { get; set; } = new();
        public bool IsFallback { get; set; }
    }
}