// LegalMateAI.BLL/Services/IService/IContractService.cs
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IContractService
    {
        // ========== القوالب ==========
        Task<List<ContractTemplateResponseDto>> GetContractTemplatesAsync(ContractType? type = null, string? search = null);
        Task<ContractTemplateResponseDto?> GetTemplateByIdAsync(Guid templateId);
        
        // ========== توليد العقود ==========
        Task<ContractResponseDto?> GenerateContractFromTemplateAsync(Guid userId, GenerateContractRequest request);
        
        // ========== عقود المستخدم ==========
        Task<List<ContractResponseDto>> GetUserContractsAsync(Guid userId, string? status = null, string? search = null);
        
        // ========== بحث وعرض عام (Public) ==========
        Task<List<ContractResponseDto>> SearchAllContractsAsync(string searchTerm);
        Task<ContractResponseDto?> GetAnyContractByIdAsync(Guid contractId);
        
        // ========== تعديل وحذف (مالك العقد فقط) ==========
        Task<ContractResponseDto?> UpdateContractAsync(Guid userId, Guid contractId, UpdateContractDto request);
        Task<bool> UpdateContractStatusAsync(Guid userId, Guid contractId, UpdateContractStatusDto request);
        Task<bool> DeleteContractAsync(Guid userId, Guid contractId);
        
        // ========== تحميل ==========
        Task<byte[]?> DownloadAnyContractAsync(Guid contractId);
    }

    public class GenerateContractRequest
    {
        public Guid TemplateId { get; set; }
        public Dictionary<string, string> FilledData { get; set; } = new();
        public string? ContractTitle { get; set; }
        public Guid? LawyerId { get; set; }
    }
}