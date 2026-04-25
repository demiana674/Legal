using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using Microsoft.AspNetCore.Http;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IAdminProfileService
    {
        Task<AdminProfileDto?> GetProfileAsync(Guid adminId);
        Task<bool> UpdateProfileAsync(Guid adminId, UpdateAdminProfileDto request);
        Task<string?> UploadProfilePictureAsync(Guid adminId, IFormFile file);
        Task<bool> RemoveProfilePictureAsync(Guid adminId);
        Task<AdminDashboardDto?> GetDashboardAsync(Guid adminId);
    }
}