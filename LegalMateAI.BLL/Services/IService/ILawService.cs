// LegalMateAI.BLL/Services/IService/ILawService.cs
using Microsoft.AspNetCore.Http;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ILawService
    {
        Task<List<LawDto>> GetLawsAsync(LawCategory? category = null, string? search = null);
        Task<List<LawDto>> SearchLawsAsync(string searchTerm);
        Task<LawDto?> GetLawByIdAsync(Guid id);
        Task<byte[]?> DownloadLawAsync(Guid id);
        Task<string?> GetLawDownloadUrlAsync(Guid id);
        Task<object?> GetLawDownloadInfoAsync(Guid id);
        Task<List<LawCategoryDto>> GetLawCategoriesAsync();
        Task<LawDto?> UploadLawByUserWithParserAsync(Guid userId, CreateLawRequestDto request);
        Task<LawDto?> UploadLawByUserAsync(Guid userId, AddLawDto request);
        Task<List<LawDto>> GetUserUploadedLawsAsync(Guid userId);
        Task<LawDto?> AddLawAsync(Guid adminId, IFormFile pdfFile, string name, LawCategory category, string? lawNumber, int? year, string? description, string? sourceUrl, string? searchKeywords);
        Task<LawDto?> UpdateLawAsync(Guid adminId, Guid lawId, UpdateLawDto request);
        Task<bool> DeleteLawAsync(Guid adminId, Guid lawId);
        Task<List<LawDto>> GetAllLawsForAdminAsync();
        Task<List<LawDto>> GetPendingLawsAsync();
        Task<List<LawDto>> GetPendingLawsForAdminAsync();
        Task<bool> ApproveLawAsync(Guid adminId, Guid lawId);
        Task<bool> ApproveUserLawAsync(Guid adminId, Guid lawId);
        Task<bool> RejectLawAsync(Guid adminId, Guid lawId, string reason);
        Task<bool> RejectUserLawAsync(Guid adminId, Guid lawId, string reason);
    }
}