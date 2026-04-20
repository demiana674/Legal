// LegalMateAI.BLL/Services/IService/IAdminService.cs
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IAdminService
    {
        // Dashboard
        Task<AdminDashboardDto> GetDashboardStatsAsync(Guid adminId);
        
        // إدارة المستخدمين
        Task<List<UserResponseDto>> GetAllUsersAsync(UserFilterDto? filter = null);
        Task<UserResponseDto?> GetUserDetailsAsync(Guid userId);
        Task<bool> UpdateUserStatusAsync(Guid adminId, Guid userId, AccountStatus status, string? reason = null);
        Task<bool> DeleteUserAsync(Guid adminId, Guid userId);
        
        // ========== إدارة المحامين (المضافة من AdminLawyerService) ==========
        
        /// <summary>
        /// جلب المحامين المنتظرين للموافقة
        /// </summary>
        Task<List<PendingLawyerDto>> GetPendingLawyersAsync();
        
        /// <summary>
        /// جلب جميع المحامين مع فلترة
        /// </summary>
        Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null);
        
        /// <summary>
        /// جلب تفاصيل محامي محدد
        /// </summary>
        Task<LawyerResponseDto?> GetLawyerDetailsAsync(Guid lawyerId);
        
        /// <summary>
        /// تحديث حالة المحامي
        /// </summary>
        Task<bool> UpdateLawyerStatusAsync(Guid userId, LawyerVerificationStatus status, string? notes = null);
        
        /// <summary>
        /// الموافقة على محامي
        /// </summary>
        Task<bool> ApproveLawyerAsync(Guid userId);
        
        /// <summary>
        /// رفض محامي
        /// </summary>
        Task<bool> RejectLawyerAsync(Guid userId, string reason);
        
        /// <summary>
        /// تعليق محامي
        /// </summary>
        Task<bool> SuspendLawyerAsync(Guid userId, string? reason = null);
        
        /// <summary>
        /// تنشيط محامي
        /// </summary>
        Task<bool> ActivateLawyerAsync(Guid userId);
        
        /// <summary>
        /// حذف محامي
        /// </summary>
        Task<bool> DeleteLawyerAsync(Guid userId);
        
        // ========== دوال الموافقة القديمة (للتوافق) ==========
        Task<bool> VerifyLawyerAsync(Guid adminId, Guid lawyerId, bool isApproved, string? rejectionReason = null);
        Task<bool> SuspendLawyerAsync(Guid adminId, Guid lawyerId, string? reason = null);
        Task<bool> ActivateLawyerAsync(Guid adminId, Guid lawyerId);
        Task<bool> DeleteLawyerAsync(Guid adminId, Guid lawyerId);
        
        // إدارة السجلات
        Task<List<AdminLogDto>> GetAdminLogsAsync(LogFilterDto? filter = null);
        Task<byte[]> ExportLogsAsync(LogFilterDto? filter = null);
        Task<byte[]> ExportLogsAsync(LogFilterDto? filter, string format);
        Task<byte[]> ExportLogsToPdfAsync(LogFilterDto? filter = null);
        
        // إدارة النظام
        Task<SystemStatsDto> GetSystemStatsAsync();
        Task<bool> ClearCacheAsync(Guid adminId);
    }
}