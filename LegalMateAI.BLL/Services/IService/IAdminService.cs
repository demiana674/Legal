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
        
        //  دوال تعليق وتفعيل المستخدمين
        Task<bool> SuspendUserAsync(Guid adminId, Guid userId, string? reason = null);
        Task<bool> ActivateUserAsync(Guid adminId, Guid userId);
        Task<List<UserResponseDto>> GetSuspendedUsersAsync();

        // ==================== Lawyer Management ====================
        Task<List<PendingLawyerDto>> GetPendingLawyersAsync();
        Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null);
        Task<LawyerResponseDto?> GetLawyerDetailsAsync(Guid lawyerId);
        
        Task<bool> ApproveLawyerAsync(Guid userId);
        Task<bool> RejectLawyerAsync(Guid userId, string reason);
        Task<bool> SuspendLawyerAsync(Guid userId, string? reason = null);
        Task<bool> ActivateLawyerAsync(Guid userId);
        Task<List<LawyerResponseDto>> GetSuspendedLawyersAsync();
        Task<bool> DeleteLawyerAsync(Guid userId);

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