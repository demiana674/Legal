using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.DTOs.CreateDTO;
using System.Security.Claims;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/profile")]
    public class AdminProfileController : ControllerBase
    {
        private readonly IAdminProfileService _profileService;
        private readonly ILogService _logService;
        private readonly ILogger<AdminProfileController> _logger;

        public AdminProfileController(
            IAdminProfileService profileService,
            ILogService logService,
            ILogger<AdminProfileController> logger)
        {
            _profileService = profileService;
            _logService = logService;
            _logger = logger;
        }

        private Guid GetAdminId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id") ?? User.FindFirst("sub");
            if (claim == null) throw new UnauthorizedAccessException("لم يتم العثور على معرف الأدمن");
            return Guid.Parse(claim.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var profile = await _profileService.GetProfileAsync(GetAdminId());
            return Ok(profile);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var dashboard = await _profileService.GetDashboardAsync(GetAdminId());
            return Ok(dashboard);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAdminProfileDto request)
        {
            var adminId = GetAdminId();
            var result = await _profileService.UpdateProfileAsync(adminId, request);
            if (!result) return BadRequest(new { message = "فشل تحديث البيانات" });

            // ✅ تسجيل تحديث الملف الشخصي
            await _logService.LogActionAsync(adminId, AdminLogAction.UpdateProfile, "Admin", adminId);
            _logger.LogInformation("Admin {AdminId} updated profile", adminId);

            return Ok(new { message = "تم تحديث البيانات بنجاح" });
        }

        [HttpPost("picture")]
        public async Task<IActionResult> UploadPicture([FromForm] UploadProfilePictureDto request)
        {
            if (request.ProfilePicture == null) return BadRequest(new { message = "الصورة مطلوبة" });
            
            var adminId = GetAdminId();
            var pictureUrl = await _profileService.UploadProfilePictureAsync(adminId, request.ProfilePicture);
            if (pictureUrl == null) return BadRequest(new { message = "فشل رفع الصورة" });

            // ✅ تسجيل رفع الصورة
            await _logService.LogActionAsync(adminId, AdminLogAction.UpdateProfile, "Admin", adminId);

            return Ok(new { pictureUrl });
        }

        [HttpDelete("picture")]
        public async Task<IActionResult> RemovePicture()
        {
            var adminId = GetAdminId();
            var result = await _profileService.RemoveProfilePictureAsync(adminId);
            if (!result) return BadRequest(new { message = "فشل حذف الصورة" });

            // ✅ تسجيل حذف الصورة
            await _logService.LogActionAsync(adminId, AdminLogAction.UpdateProfile, "Admin", adminId);

            return Ok(new { message = "تم حذف الصورة بنجاح" });
        }
    }
}