// LegalMateAI.BLL/Services/IService/ILawService.cs
using Microsoft.AspNetCore.Http;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ILawService
    {
        // ========== للجميع ==========
        Task<List<LawDto>> GetLawsAsync(LawCategory? category = null, string? search = null);
        Task<List<LawDto>> SearchLawsAsync(string searchTerm);
        Task<LawDto?> GetLawByIdAsync(Guid id);
        Task<byte[]?> DownloadLawAsync(Guid id);
        Task<string?> GetLawDownloadUrlAsync(Guid id);
        Task<List<LawCategoryDto>> GetLawCategoriesAsync();

        // ========== للمستخدمين المسجلين ==========
        Task<LawDto?> UploadLawByUserAsync(Guid? userId, AddLawDto request);
        Task<List<LawDto>> GetUserUploadedLawsAsync(Guid userId);

        // ========== للأدمن فقط ==========
        Task<List<LawDto>> GetPendingLawsAsync();
        Task<bool> ApproveLawAsync(Guid adminId, Guid lawId);
        Task<bool> RejectLawAsync(Guid adminId, Guid lawId, string reason);
    }
}