using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using Microsoft.AspNetCore.Http;
namespace LegalMateAI.BLL.Services.IService
{
    public interface ILawyerProfileService
    {
        Task<LawyerProfileDto?> GetProfileAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateLawyerProfileDto request);
        Task<string?> UploadProfilePictureAsync(Guid userId, IFormFile file);
        Task<bool> RemoveProfilePictureAsync(Guid userId);
        Task<LawyerProfileDto?> GetDashboardAsync(Guid userId);
    }
}