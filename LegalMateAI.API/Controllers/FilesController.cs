// LegalMateAI.API/Controllers/FilesController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class FilesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public FilesController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("admin-profile-picture/{fileName}")]
        public IActionResult GetAdminProfilePicture(string fileName)
        {
            var path = Path.Combine(_env.WebRootPath, "profiles", "admins", fileName);
            
            if (!System.IO.File.Exists(path))
                return NotFound(new { message = "الصورة غير موجودة", path });

            var fileBytes = System.IO.File.ReadAllBytes(path);
            var contentType = GetContentType(fileName);
            
            return File(fileBytes, contentType);
        }

        [HttpGet("lawyer-profile-picture/{fileName}")]
        public IActionResult GetLawyerProfilePicture(string fileName)
        {
            var path = Path.Combine(_env.WebRootPath, "profiles", "lawyers", fileName);
            
            if (!System.IO.File.Exists(path))
                return NotFound(new { message = "الصورة غير موجودة", path });

            var fileBytes = System.IO.File.ReadAllBytes(path);
            var contentType = GetContentType(fileName);
            
            return File(fileBytes, contentType);
        }

        [HttpGet("user-profile-picture/{fileName}")]
        public IActionResult GetUserProfilePicture(string fileName)
        {
            var path = Path.Combine(_env.WebRootPath, "profiles", "users", fileName);
            
            if (!System.IO.File.Exists(path))
                return NotFound(new { message = "الصورة غير موجودة", path });

            var fileBytes = System.IO.File.ReadAllBytes(path);
            var contentType = GetContentType(fileName);
            
            return File(fileBytes, contentType);
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}