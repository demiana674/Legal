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
        
        // User Management
        Task<List<UserResponseDto>> GetAllUsersAsync(UserFilterDto? filter = null);
        Task<UserResponseDto?> GetUserDetailsAsync(Guid userId);
        Task<bool> UpdateUserStatusAsync(Guid adminId, Guid userId, AccountStatus status, string? reason = null);
        Task<bool> DeleteUserAsync(Guid adminId, Guid userId);
        
        // Lawyer Management
        Task<List<PendingLawyerDto>> GetPendingLawyersAsync();
        Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null);
        Task<LawyerResponseDto?> GetLawyerDetailsAsync(Guid lawyerId);
        Task<LawyerResponseDto?> GetLawyerDetailsByIdAsync(Guid lawyerId);  // New
        Task<bool> ApproveLawyerAsync(Guid userId);
        Task<bool> RejectLawyerAsync(Guid userId, string reason);
        Task<bool> SuspendLawyerAsync(Guid userId, string? reason = null);
        Task<bool> ActivateLawyerAsync(Guid userId);
        Task<bool> DeleteLawyerAsync(Guid userId);
        
        // Admin Details
        Task<AdminProfileDto?> GetAdminDetailsAsync(Guid adminId);
        Task<AdminProfileDto?> GetAdminDetailsByIdAsync(Guid adminId);  // New
        
        // Entity Details
        Task<object?> GetEntityDetailsAsync(Guid id);
        
        // Log Management
        Task<List<AdminLogDto>> GetLogsAsync(LogFilterDto? filter = null);
        Task<List<AdminLogDto>> GetAdminLogsAsync(LogFilterDto? filter = null);
        Task<byte[]> ExportLogsAsync(LogFilterDto? filter, string format = "csv");
        Task<byte[]> ExportLogsToPdfAsync(LogFilterDto? filter = null);
        Task<List<AdminLogDto>> GetAllUserLogsAsync(LogFilterDto? filter = null);
        Task<List<AdminLogDto>> GetUserLogsAsync(Guid userId, LogFilterDto? filter = null);
        Task<SystemLogsStatsDto> GetLogsStatsAsync();
        
        // System Management
        Task<SystemStatsDto> GetSystemStatsAsync();
        Task<bool> ClearCacheAsync(Guid adminId);
    }
}