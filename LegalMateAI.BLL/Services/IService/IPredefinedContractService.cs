// LegalMateAI.BLL/Services/IService/IPredefinedContractService.cs
using Microsoft.AspNetCore.Http;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IPredefinedContractService
    {
        // ========== Admin Operations ==========
        
        /// <summary>
        /// رفع قالب جديد (يدعم PDF و Word)
        /// </summary>
        Task<PredefinedContractTemplateDto?> UploadTemplateAsync(
            Guid adminId, 
            IFormFile file,  // PDF أو Word
            string name, 
            string? nameEn,
            string? description, 
            ContractType contractType,
            List<string> requiredFields,
            string? searchKeywords = null,
            bool isFeatured = false);
        
        /// <summary>
        /// تحديث قالب موجود
        /// </summary>
        Task<PredefinedContractTemplateDto?> UpdateTemplateAsync(
            Guid adminId,
            Guid templateId,
            string? name,
            string? nameEn,
            string? description,
            bool? isActive,
            bool? isFeatured,
            List<string>? requiredFields,
            string? searchKeywords);
        
        /// <summary>
        /// حذف قالب
        /// </summary>
        Task<bool> DeleteTemplateAsync(Guid adminId, Guid templateId);
        
        /// <summary>
        /// جلب كل القوالب (للأدمن)
        /// </summary>
        Task<List<PredefinedContractTemplateDto>> GetAllTemplatesForAdminAsync(
            bool includeInactive = true, 
            ContractType? type = null,
            string? searchTerm = null);
        
        // ========== User Operations ==========
        
        /// <summary>
        /// جلب القوالب النشطة مع إمكانية البحث
        /// </summary>
        Task<List<PredefinedContractTemplateDto>> GetActiveTemplatesAsync(
            ContractType? type = null, 
            string? searchTerm = null,
            bool featuredOnly = false);
        
        /// <summary>
        /// جلب أكثر القوالب استخداماً
        /// </summary>
        Task<List<PredefinedContractTemplateDto>> GetPopularTemplatesAsync(int count = 5);
        
        /// <summary>
        /// جلب القوالب المميزة
        /// </summary>
        Task<List<PredefinedContractTemplateDto>> GetFeaturedTemplatesAsync();
        
        /// <summary>
        /// البحث في القوالب (بالاسم أو النوع أو الكلمات المفتاحية)
        /// </summary>
        Task<List<PredefinedContractTemplateDto>> SearchTemplatesAsync(string searchTerm, ContractType? type = null);
        
        /// <summary>
        /// جلب تفاصيل قالب واحد
        /// </summary>
        Task<PredefinedContractTemplateDto?> GetTemplateByIdAsync(Guid templateId);
        
        /// <summary>
        /// توليد عقد من قالب (Word أو PDF)
        /// </summary>
        Task<GeneratedContractDto?> GenerateContractFromTemplateAsync(
            Guid userId,
            Guid templateId,
            Dictionary<string, string> filledData,
            Guid? lawyerId = null,
            string outputFormat = "pdf");  // "pdf" أو "docx"
        
        /// <summary>
        /// تحميل العقد المولد
        /// </summary>
        Task<byte[]?> DownloadGeneratedContractAsync(Guid userId, Guid contractId);
        
        /// <summary>
        /// جلب عقود المستخدم المولدة
        /// </summary>
        Task<List<GeneratedContractDto>> GetUserGeneratedContractsAsync(Guid userId);
        
        /// <summary>
        /// حذف عقد مولّد
        /// </summary>
        Task<bool> DeleteGeneratedContractAsync(Guid userId, Guid contractId);
    }

    /// <summary>
    /// نموذج البحث عن القوالب
    /// </summary>
    public class TemplateSearchDto
    {
        public string? SearchTerm { get; set; }
        public ContractType? ContractType { get; set; }
        public bool FeaturedOnly { get; set; }
        public bool PopularOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}