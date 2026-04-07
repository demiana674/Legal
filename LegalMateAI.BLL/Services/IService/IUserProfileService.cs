using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using Microsoft.AspNetCore.Http;
namespace LegalMateAI.BLL.Services.IService
{
    public interface IUserProfileService
    {
        Task<UserProfileDto?> GetProfileAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateUserProfileDto request);
        Task<string?> UploadProfilePictureAsync(Guid userId, IFormFile file);
        Task<bool> RemoveProfilePictureAsync(Guid userId);
        Task<UserProfileDto?> GetDashboardAsync(Guid userId);
    }
}