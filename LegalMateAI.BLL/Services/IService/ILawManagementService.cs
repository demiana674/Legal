using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ILawManagementService
    {
        Task<Guid?> AddLawAsync(Guid adminId, CreateLawDto request);
        Task<bool> RemoveLawAsync(Guid adminId, Guid lawId);                
    }
}