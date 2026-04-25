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
        
        // إدارة المحامين
        Task<List<PendingLawyerDto>> GetPendingLawyersAsync();
        Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null);
        Task<LawyerResponseDto?> GetLawyerDetailsAsync(Guid lawyerId);
        Task<bool> ApproveLawyerAsync(Guid userId);
        Task<bool> RejectLawyerAsync(Guid userId, string reason);
        Task<bool> SuspendLawyerAsync(Guid userId, string? reason = null);
        Task<bool> ActivateLawyerAsync(Guid userId);
        Task<bool> DeleteLawyerAsync(Guid userId);
        
        // إدارة السجلات (Admin Logs)
        Task<List<AdminLogDto>> GetAdminLogsAsync(LogFilterDto? filter = null);
        Task<byte[]> ExportLogsAsync(LogFilterDto? filter, string format = "csv");
        Task<byte[]> ExportLogsToPdfAsync(LogFilterDto? filter = null);
        
        // 🆕 سجلات المستخدمين (All Users Activity)
        Task<List<AdminLogDto>> GetAllUserLogsAsync(LogFilterDto? filter = null);
        Task<List<AdminLogDto>> GetUserLogsAsync(Guid userId, LogFilterDto? filter = null);
        Task<SystemLogsStatsDto> GetLogsStatsAsync();
        
        // إدارة النظام
        Task<SystemStatsDto> GetSystemStatsAsync();
        Task<bool> ClearCacheAsync(Guid adminId);
    }
}