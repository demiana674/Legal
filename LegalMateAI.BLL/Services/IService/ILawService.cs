using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ILawService
    {
        // ========== للجميع ==========
        Task<List<LawCompleteDto>> GetLawsAsync(LawCategory? category = null, string? search = null);
        Task<List<LawCompleteDto>> SearchLawsAsync(string searchTerm);
        Task<LawCompleteDto?> GetLawByIdAsync(Guid id);
        Task<byte[]?> DownloadLawAsync(Guid id);
        Task<string?> GetLawDownloadUrlAsync(Guid id);
        Task<List<LawCategoryDto>> GetLawCategoriesAsync();

        // ========== أجزاء منفصلة (جديد) ==========
        Task<LawCoreInfoDto?> GetLawCoreInfoAsync(Guid id);
        Task<LawFileLinksDto?> GetLawFileLinksAsync(Guid id);
        Task<LawMetricsDto?> GetLawMetricsAsync(Guid id);
        Task<LawAuditDto?> GetLawAuditAsync(Guid id);
        Task<LawContentDto?> GetLawContentAsync(Guid id);

        // ========== للمستخدمين المسجلين ==========
        Task<LawCompleteDto?> UploadLawByUserAsync(Guid? userId, AddLawDto request);
        Task<List<LawCompleteDto>> GetUserUploadedLawsAsync(Guid userId);

        // ========== للأدمن فقط ==========
        Task<List<LawCompleteDto>> GetPendingLawsAsync();
        Task<bool> ApproveLawAsync(Guid adminId, Guid lawId);
        Task<bool> RejectLawAsync(Guid adminId, Guid lawId, string reason);
    }
}