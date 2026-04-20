// LegalMateAI.BLL/Services/IService/IContractService.cs
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IContractService
    {
        // ========== القوالب (للقراءة فقط) ==========
        
        /// <summary>
        /// جلب القوالب المتاحة
        /// </summary>
        Task<List<ContractTemplateResponseDto>> GetContractTemplatesAsync(ContractType? type = null, string? search = null);
        
        /// <summary>
        /// جلب قالب محدد
        /// </summary>
        Task<ContractTemplateResponseDto?> GetTemplateByIdAsync(Guid templateId);
        
        /// <summary>
        /// تحميل ملف القالب
        /// </summary>
        Task<byte[]?> DownloadTemplateAsync(Guid templateId);
        
        // ========== عقود المستخدم ==========
        
        /// <summary>
        /// توليد عقد من قالب
        /// </summary>
        Task<ContractResponseDto?> GenerateContractFromTemplateAsync(Guid userId, GenerateContractRequest request);
        
        /// <summary>
        /// جلب عقود المستخدم
        /// </summary>
        Task<List<ContractResponseDto>> GetUserContractsAsync(Guid userId, string? status = null, string? search = null);
        
        /// <summary>
        /// جلب عقود المحامي
        /// </summary>
        Task<List<ContractResponseDto>> GetLawyerContractsAsync(Guid lawyerId, string? status = null, string? search = null);
        
        /// <summary>
        /// البحث في العقود
        /// </summary>
        Task<List<ContractResponseDto>> SearchContractsAsync(Guid userId, string searchTerm, bool isLawyer);
        
        /// <summary>
        /// جلب عقد محدد
        /// </summary>
        Task<ContractResponseDto?> GetContractByIdAsync(Guid userId, Guid contractId, bool isLawyer = false);
        
        /// <summary>
        /// تحديث بيانات العقد
        /// </summary>
        Task<ContractResponseDto?> UpdateContractAsync(Guid userId, Guid contractId, UpdateContractDto request);
        
        /// <summary>
        /// تحديث حالة العقد
        /// </summary>
        Task<bool> UpdateContractStatusAsync(Guid userId, Guid contractId, UpdateContractStatusDto request, bool isLawyer = false);
        
        /// <summary>
        /// حذف عقد
        /// </summary>
        Task<bool> DeleteContractAsync(Guid userId, Guid contractId);
        
        /// <summary>
        /// تحميل ملف العقد
        /// </summary>
        Task<byte[]?> DownloadContractAsync(Guid userId, Guid contractId);
    }

    /// <summary>
    /// نموذج طلب توليد عقد من قالب
    /// </summary>
    public class GenerateContractRequest
    {
        public Guid TemplateId { get; set; }
        public Dictionary<string, string> FilledData { get; set; } = new();
        public string? ContractTitle { get; set; }
        public Guid? LawyerId { get; set; }
    }
}