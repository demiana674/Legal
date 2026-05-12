// LegalMateAI.BLL/Services/IService/IAdminService.cs
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IAdminService
    {
        // ==================== Dashboard ====================
        Task<AdminDashboardDto> GetDashboardStatsAsync(Guid adminId);

        // ==================== User Management ====================
        Task<List<UserResponseDto>> GetAllUsersAsync(UserFilterDto? filter = null);
        Task<UserResponseDto?> GetUserDetailsAsync(Guid userId);
        Task<bool> UpdateUserStatusAsync(Guid adminId, Guid userId, AccountStatus status, string? reason = null);
        Task<bool> DeleteUserAsync(Guid adminId, Guid userId);

        // ==================== Lawyer Management ====================
        Task<List<PendingLawyerDto>> GetPendingLawyersAsync();
        Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null);
        Task<LawyerResponseDto?> GetLawyerDetailsAsync(Guid lawyerId);
        
        /// <summary>
        /// ✅ الموافقة على محامي - تغيير الحالة من Pending إلى Active
        /// </summary>
        Task<bool> ApproveLawyerAsync(Guid userId);
        
        /// <summary>
        /// ✅ رفض محامي - حذف نهائي من النظام
        /// </summary>
        Task<bool> RejectLawyerAsync(Guid userId, string reason);
        
        /// <summary>
        /// ✅ تعليق محامي (للحسابات النشطة)
        /// </summary>
        Task<bool> SuspendLawyerAsync(Guid userId, string? reason = null);
        
        /// <summary>
        /// ✅ إعادة تنشيط محامي
        /// </summary>
        Task<bool> ActivateLawyerAsync(Guid userId);
        
        /// <summary>
        /// ✅ حذف محامي نهائي
        /// </summary>
        Task<bool> DeleteLawyerAsync(Guid userId);
        
        /// <summary>
        /// ✅ تحديث حالة المحامي العامة
        /// </summary>
        Task<bool> UpdateLawyerStatusAsync(Guid userId, LawyerVerificationStatus status, string? notes = null);

        // ==================== Log Management ====================
        Task<PaginatedLogsDto<AdminLogDto>> GetAllLogsAsync(LogFilterDto? filter = null);
        Task<PaginatedLogsDto<AdminLogDto>> GetUserLogsAsync(Guid userId, LogFilterDto? filter = null);
        Task<SystemLogsStatsDto> GetLogsStatsAsync();

        // ==================== Export Methods ====================
        Task<byte[]> ExportLogsAsync(LogFilterDto? filter, string format = "csv");
        Task<byte[]> ExportLogsToPdfAsync(LogFilterDto? filter = null);

        // ==================== System Management ====================
        Task<SystemStatsDto> GetSystemStatsAsync();
        Task<bool> ClearCacheAsync(Guid adminId);
    }
}