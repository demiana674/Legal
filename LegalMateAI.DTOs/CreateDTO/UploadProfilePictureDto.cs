using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.CreateDTO
{
    public class UploadProfilePictureDto
    {
        [Required]
        public IFormFile ProfilePicture { get; set; } = null!;
    }
}