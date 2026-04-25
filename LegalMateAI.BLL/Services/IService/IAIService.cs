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
        /// تحليل مستند قانوني
        /// </summary>
        Task<AIAnalysisResult> AnalyzeDocumentAsync(Document document, byte[] fileContent);
        
        /// <summary>
        /// تحليل سريع (ملخص + مخاطر فقط)
        /// </summary>
        Task<QuickAnalysisResult> QuickAnalysisAsync(string text);
        
        /// <summary>
        /// إنشاء عقد
        /// </summary>
        Task<string> GenerateContractAsync(ContractTemplate template, Dictionary<string, string> data);
        
        /// <summary>
        /// اقتراح محامين
        /// </summary>
        Task<List<LegalMateAI.DTOs.ReadDTO.LawyerSuggestionDto>> SuggestLawyersAsync(string documentContent, string specialization);
        
        /// <summary>
        /// بحث ذكي
        /// </summary>
        Task<List<LegalMateAI.DTOs.ReadDTO.SearchResultDto>> SmartSearchAsync(string query, int limit = 10);
        
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
        public List<LegalMateAI.DTOs.ReadDTO.LawyerSuggestionDto> SuggestedLawyers { get; set; } = new();
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