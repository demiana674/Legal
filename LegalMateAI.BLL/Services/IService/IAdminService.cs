// LegalMateAI.BLL/Services/IService/IAdminService.cs
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IAdminService
    {
        Task<AdminDashboardDto> GetDashboardStatsAsync(Guid adminId);
        Task<List<UserResponseDto>> GetAllUsersAsync(UserFilterDto? filter = null);
        Task<bool> UpdateUserStatusAsync(Guid adminId, Guid userId, AccountStatus status, string? reason = null);
        Task<bool> DeleteUserAsync(Guid adminId, Guid userId);
        Task<List<PendingLawyerDto>> GetPendingLawyersAsync();
        Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null);
        Task<bool> ApproveLawyerAsync(Guid userId);
        Task<bool> RejectLawyerAsync(Guid userId, string reason);
        Task<bool> SuspendLawyerAsync(Guid userId, string? reason = null);
        Task<bool> ActivateLawyerAsync(Guid userId);
        Task<bool> DeleteLawyerAsync(Guid userId);
        Task<object?> GetEntityDetailsAsync(Guid id);
        Task<AdminProfileDto?> GetAdminDetailsAsync(Guid adminId);
        Task<List<AdminLogDto>> GetLogsAsync(LogFilterDto? filter = null);
        Task<byte[]> ExportLogsAsync(LogFilterDto? filter, string format = "csv");
        Task<SystemLogsStatsDto> GetLogsStatsAsync();
        Task<SystemStatsDto> GetSystemStatsAsync();
        Task<bool> ClearCacheAsync(Guid adminId);
    }
}