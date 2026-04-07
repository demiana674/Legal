// LegalMateAI.BLL/Services/IService/IAdminLawyerService.cs
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IAdminLawyerService
    {
        // جلب المحامين المنتظرين
        Task<List<PendingLawyerDto>> GetPendingLawyersAsync();
        
        // جلب جميع المحامين مع فلترة
        Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null);
        
        // جلب محامي واحد (باستخدام UserId)
        Task<LawyerResponseDto?> GetLawyerByIdAsync(Guid userId);
        
        // ✅ تحديث حالة المحامي (باستخدام UserId)
        Task<bool> UpdateLawyerStatusAsync(Guid userId, LawyerVerificationStatus status, string? notes = null);
        
        // ✅ الموافقة على محامي (باستخدام UserId)
        Task<bool> ApproveLawyerAsync(Guid userId);
        
        // ✅ رفض محامي (باستخدام UserId)
        Task<bool> RejectLawyerAsync(Guid userId, string reason);
        
        // ✅ تعليق محامي (باستخدام UserId)
        Task<bool> SuspendLawyerAsync(Guid userId, string? reason = null);
        
        // ✅ إعادة تنشيط محامي (باستخدام UserId)
        Task<bool> ActivateLawyerAsync(Guid userId);
        
        // ✅ حذف محامي (باستخدام UserId)
        Task<bool> DeleteLawyerAsync(Guid userId);
    }
}