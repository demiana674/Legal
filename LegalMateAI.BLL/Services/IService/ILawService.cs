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
        // ========== للجميع (Guest والمستخدمين) ==========
        
        /// <summary>
        /// جلب القوانين المتاحة (المعتمدة فقط)
        /// </summary>
        Task<List<LawDto>> GetLawsAsync(LawCategory? category = null, string? search = null);
        
        /// <summary>
        /// البحث في القوانين
        /// </summary>
        Task<List<LawDto>> SearchLawsAsync(string searchTerm);
        
        /// <summary>
        /// جلب قانون محدد
        /// </summary>
        Task<LawDto?> GetLawByIdAsync(Guid id);
        
        /// <summary>
        /// تحميل ملف القانون (يرجع بايتات الملف)
        /// </summary>
        Task<byte[]?> DownloadLawAsync(Guid id);
        
        /// <summary>
        /// جلب رابط تحميل القانون (يرجع الرابط الخارجي)
        /// </summary>
        Task<string?> GetLawDownloadUrlAsync(Guid id);
        
        /// <summary>
        /// جلب تصنيفات القوانين المتاحة
        /// </summary>
        Task<List<LawCategoryDto>> GetLawCategoriesAsync();
        
        // ========== للمستخدمين المسجلين ==========
        
        /// <summary>
        /// رفع قانون جديد مع بارسر آلي من الرابط
        /// </summary>
        Task<LawDto?> UploadLawByUserWithParserAsync(Guid userId, CreateLawRequestDto request);
        
        /// <summary>
        /// رفع قانون جديد من قبل المستخدم (ينتظر موافقة الأدمن)
        /// </summary>
        Task<LawDto?> UploadLawByUserAsync(Guid userId, AddLawDto request);
        
        /// <summary>
        /// جلب القوانين اللي رفعها المستخدم
        /// </summary>
        Task<List<LawDto>> GetUserUploadedLawsAsync(Guid userId);
        
        // ========== للأدمن فقط ==========
        
        /// <summary>
        /// إضافة قانون جديد (Admin)
        /// </summary>
        Task<LawDto?> AddLawAsync(Guid adminId, IFormFile pdfFile, string name, 
            LawCategory category, string? lawNumber, int? year, 
            string? description, string? sourceUrl, string? searchKeywords);
        
        /// <summary>
        /// تحديث قانون (Admin)
        /// </summary>
        Task<LawDto?> UpdateLawAsync(Guid adminId, Guid lawId, UpdateLawDto request);
        
        /// <summary>
        /// حذف قانون (Admin)
        /// </summary>
        Task<bool> DeleteLawAsync(Guid adminId, Guid lawId);
        
        /// <summary>
        /// جلب كل القوانين للأدمن
        /// </summary>
        Task<List<LawDto>> GetAllLawsForAdminAsync();
        
        /// <summary>
        /// جلب القوانين المنتظرة للموافقة (Admin)
        /// </summary>
        Task<List<LawDto>> GetPendingLawsAsync();
        
        /// <summary>
        /// جلب القوانين المنتظرة للموافقة من المستخدمين (Admin)
        /// </summary>
        Task<List<LawDto>> GetPendingLawsForAdminAsync();
        
        /// <summary>
        /// الموافقة على قانون (Admin)
        /// </summary>
        Task<bool> ApproveLawAsync(Guid adminId, Guid lawId);
        
        /// <summary>
        /// الموافقة على قانون من مستخدم (Admin)
        /// </summary>
        Task<bool> ApproveUserLawAsync(Guid adminId, Guid lawId);
        
        /// <summary>
        /// رفض قانون (Admin)
        /// </summary>
        Task<bool> RejectLawAsync(Guid adminId, Guid lawId, string reason);
        
        /// <summary>
        /// رفض قانون من مستخدم (Admin)
        /// </summary>
        Task<bool> RejectUserLawAsync(Guid adminId, Guid lawId, string reason);
    }
}