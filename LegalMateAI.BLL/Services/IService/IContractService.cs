// LegalMateAI.BLL/Services/IService/IContractService.cs
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;
namespace LegalMateAI.BLL.Services.IService
{
    public interface IContractService
    {
        // إنشاء عقد جديد
        Task<ContractResponseDto?> CreateContractAsync(Guid userId, CreateContractDto request);
        
        // إنشاء عقد من قالب
        Task<ContractResponseDto?> CreateContractFromTemplateAsync(Guid userId, Guid templateId, Dictionary<string, string> customFields);
        
        // جلب عقود المستخدم
        Task<List<ContractResponseDto>> GetUserContractsAsync(Guid userId, string? status = null);
        
        // جلب عقود المحامي (للمحامي)
        Task<List<ContractResponseDto>> GetLawyerContractsAsync(Guid lawyerId, string? status = null);
        
        // جلب عقد محدد
        Task<ContractResponseDto?> GetContractByIdAsync(Guid userId, Guid contractId, bool isLawyer = false);
        
        // تحديث عقد
        Task<ContractResponseDto?> UpdateContractAsync(Guid userId, Guid contractId, UpdateContractDto request);
        
        // تحديث حالة العقد
        Task<bool> UpdateContractStatusAsync(Guid userId, Guid contractId, UpdateContractStatusDto request, bool isLawyer = false);
        
        // حذف عقد
        Task<bool> DeleteContractAsync(Guid userId, Guid contractId);
        
        // تحميل العقد
        Task<byte[]?> DownloadContractAsync(Guid userId, Guid contractId, string format = "pdf");
        
        // الحصول على قوالب العقود
        Task<List<ContractTemplateResponseDto>> GetContractTemplatesAsync(ContractType? type = null);
        
        // إنشاء قالب عقد جديد (للمدير)
        Task<ContractTemplateResponseDto?> CreateContractTemplateAsync(Guid adminId, CreateContractTemplateDto request);
        
        // تحديث قالب عقد
        Task<ContractTemplateResponseDto?> UpdateContractTemplateAsync(Guid adminId, Guid templateId, UpdateContractTemplateDto request);
        
        // حذف قالب عقد
        Task<bool> DeleteContractTemplateAsync(Guid adminId, Guid templateId);
    }
}