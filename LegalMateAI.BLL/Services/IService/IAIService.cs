using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;

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
        /// ✅ محادثة مع المساعد القانوني
        /// </summary>
Task<string> ChatAsync(string userMessage, List<(string role, string content)>? history = null);
        
        /// <summary>
        /// اقتراح محامين
        /// </summary>
        Task<List<LawyerSuggestionDto>> SuggestLawyersAsync(string documentContent, string specialization);
        
        /// <summary>
        /// بحث ذكي
        /// </summary>
        Task<List<SearchResultDto>> SmartSearchAsync(string query, int limit = 10);
        
        /// <summary>
        /// التحقق من صحة الخدمة
        /// </summary>
        Task<bool> HealthCheckAsync();
    }
    
    // ... باقي الـ DTOs كما هي ...
}