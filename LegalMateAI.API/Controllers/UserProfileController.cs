using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.DTOs.CreateDTO;
using System.Security.Claims;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.API.Controllers
{
    [Authorize(Roles = "User")]
    [ApiController]
    [Route("api/user/profile")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _profileService;
        private readonly ILogService _logService;

        public UserProfileController(IUserProfileService profileService, ILogService logService)
        {
            _profileService = profileService;
            _logService = logService;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var profile = await _profileService.GetProfileAsync(GetUserId());
            return Ok(profile);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var dashboard = await _profileService.GetDashboardAsync(GetUserId());
            return Ok(dashboard);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto request)
        {
            var userId = GetUserId();
            var result = await _profileService.UpdateProfileAsync(userId, request);
            if (!result) return BadRequest(new { message = "الملف الشخصي غير موجود أو فشل تحديث البيانات" });

            // ✅ تسجيل تحديث الملف الشخصي للمستخدم
            await _logService.LogActionAsync(userId, AdminLogAction.UpdateProfile, "User", userId);

            return Ok(new { message = "تم تحديث البيانات بنجاح" });
        }

        [HttpPost("picture")]
        public async Task<IActionResult> UploadPicture([FromForm] UploadProfilePictureDto request)
        {
            var userId = GetUserId();
            var pictureUrl = await _profileService.UploadProfilePictureAsync(userId, request.ProfilePicture);
            if (pictureUrl == null) return BadRequest(new { message = "فشل رفع الصورة" });

            // ✅ تسجيل رفع الصورة
            await _logService.LogActionAsync(userId, AdminLogAction.UpdateProfile, "User", userId);

            return Ok(new { pictureUrl });
        }

        [HttpDelete("picture")]
        public async Task<IActionResult> RemovePicture()
        {
            var userId = GetUserId();
            var result = await _profileService.RemoveProfilePictureAsync(userId);
            if (!result) return BadRequest(new { message = "فشل حذف الصورة" });

            // ✅ تسجيل حذف الصورة
            await _logService.LogActionAsync(userId, AdminLogAction.UpdateProfile, "User", userId);

            return Ok(new { message = "تم حذف الصورة بنجاح" });
        }
    }
}