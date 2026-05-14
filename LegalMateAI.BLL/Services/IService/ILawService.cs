using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ILawService
    {
        // ========== للجميع ==========
        Task<List<LawCoreInfoDto>> GetAllLawsAsync(LawCategory? category = null, string? search = null);
        Task<List<LawCoreInfoDto>> SearchLawsAsync(string searchTerm);
        Task<LawCoreInfoDto?> GetLawByIdAsync(Guid id);
        Task<byte[]?> DownloadLawAsync(Guid id);
        Task<string?> GetLawDownloadUrlAsync(Guid id);
        Task<List<LawCategoryDto>> GetLawCategoriesAsync();

        // ========== للمستخدمين المسجلين ==========
        Task<LawCoreInfoDto?> UploadLawByUserAsync(Guid? userId, AddLawDto request);
        Task<List<LawCoreInfoDto>> GetUserUploadedLawsAsync(Guid userId);

        // ========== للأدمن فقط (CRUD كامل) ==========
        Task<LawCoreInfoDto?> CreateLawAsync(Guid adminId, CreateLawDto request);
        Task<LawCoreInfoDto?> UpdateLawAsync(Guid adminId, Guid lawId, UpdateLawDto request);
        Task<bool> DeleteLawAsync(Guid adminId, Guid lawId);
        Task<List<LawCoreInfoDto>> GetPendingLawsAsync();
        Task<bool> ApproveLawAsync(Guid adminId, Guid lawId);
        Task<bool> RejectLawAsync(Guid adminId, Guid lawId, string reason);
    }
}