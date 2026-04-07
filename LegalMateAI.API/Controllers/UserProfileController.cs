using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.DTOs.CreateDTO;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [Authorize(Roles = "User")]
    [ApiController]
    [Route("api/user/profile")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _profileService;

        public UserProfileController(IUserProfileService profileService)
        {
            _profileService = profileService;
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
    var result = await _profileService.UpdateProfileAsync(GetUserId(), request);
    
    if (!result) 
        return BadRequest(new { message = "الملف الشخصي غير موجود أو فشل تحديث البيانات" });
    
    return Ok(new { message = "تم تحديث البيانات بنجاح" });
}

        [HttpPost("picture")]
        public async Task<IActionResult> UploadPicture([FromForm] UploadProfilePictureDto request)
        {
            var pictureUrl = await _profileService.UploadProfilePictureAsync(GetUserId(), request.ProfilePicture);
            if (pictureUrl == null) return BadRequest(new { message = "فشل رفع الصورة" });
            return Ok(new { pictureUrl });
        }

        [HttpDelete("picture")]
        public async Task<IActionResult> RemovePicture()
        {
            var result = await _profileService.RemoveProfilePictureAsync(GetUserId());
            if (!result) return BadRequest(new { message = "فشل حذف الصورة" });
            return Ok(new { message = "تم حذف الصورة بنجاح" });
        }
    }
}