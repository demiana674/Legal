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
        
        // إدارة المحامين
        Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null);
        Task<LawyerResponseDto?> GetLawyerDetailsAsync(Guid lawyerId);
        Task<bool> VerifyLawyerAsync(Guid adminId, Guid lawyerId, bool isApproved, string? rejectionReason = null);
        Task<bool> SuspendLawyerAsync(Guid adminId, Guid lawyerId, string? reason = null);
        Task<bool> ActivateLawyerAsync(Guid adminId, Guid lawyerId);
        Task<bool> DeleteLawyerAsync(Guid adminId, Guid lawyerId);
        
        // إدارة المستخدمين
        Task<List<UserResponseDto>> GetAllUsersAsync(UserFilterDto? filter = null);
        Task<UserResponseDto?> GetUserDetailsAsync(Guid userId);
        Task<bool> UpdateUserStatusAsync(Guid adminId, Guid userId, AccountStatus status, string? reason = null);
        Task<bool> DeleteUserAsync(Guid adminId, Guid userId);
        
        // إدارة السجلات
        Task<List<AdminLogDto>> GetAdminLogsAsync(LogFilterDto? filter = null);
        Task<byte[]> ExportLogsAsync(LogFilterDto? filter = null);
        
        // إدارة النظام
        Task<SystemStatsDto> GetSystemStatsAsync();
        Task<bool> ClearCacheAsync(Guid adminId);
    }

   
}