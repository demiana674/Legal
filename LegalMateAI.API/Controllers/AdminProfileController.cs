// LegalMateAI.API/Controllers/AdminProfileController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.DTOs.CreateDTO;
using System.Security.Claims;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/profile")]
    public class AdminProfileController : ControllerBase
    {
        private readonly IAdminProfileService _profileService;
        private readonly ILogger<AdminProfileController> _logger;

        public AdminProfileController(
            IAdminProfileService profileService,
            ILogger<AdminProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        // ✅ دالة موحدة لجلب AdminId من الـ Token
        private Guid GetAdminId()
        {
            // جرب ClaimTypes.NameIdentifier أولاً
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            
            // لو مش موجود، جرب "id"
            if (claim == null)
            {
                claim = User.FindFirst("id");
            }
            
            // لو مش موجود، جرب "sub"
            if (claim == null)
            {
                claim = User.FindFirst("sub");
            }
            
            if (claim == null)
            {
                _logger.LogWarning("No admin ID claim found in token. Available claims: {Claims}", 
                    string.Join(", ", User.Claims.Select(c => c.Type)));
                throw new UnauthorizedAccessException("لم يتم العثور على معرف الأدمن في التوكن");
            }
            
            if (!Guid.TryParse(claim.Value, out var adminId))
            {
                _logger.LogWarning($"Invalid admin ID format: {claim.Value}");
                throw new UnauthorizedAccessException("صيغة معرف الأدمن غير صحيحة");
            }
            
            _logger.LogInformation($"Admin ID retrieved from token: {adminId}");
            return adminId;
        }

        // GET: api/admin/profile
        [HttpGet]
        [ProducesResponseType(typeof(AdminProfileDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProfile()
        {
            var adminId = GetAdminId();
            _logger.LogInformation($"Getting profile for admin: {adminId}");
            
            var profile = await _profileService.GetProfileAsync(adminId);
            return Ok(profile);
        }

        // ✅ GET: api/admin/profile/dashboard - يعرض فقط بيانات الأدمن الحالي
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard()
        {
            var adminId = GetAdminId();
            _logger.LogInformation($"Getting dashboard for admin: {adminId}");
            
            var dashboard = await _profileService.GetDashboardAsync(adminId);
            return Ok(dashboard);
        }

        // PUT: api/admin/profile
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAdminProfileDto request)
        {
            var adminId = GetAdminId();
            _logger.LogInformation($"Updating profile for admin: {adminId}");
            
            var result = await _profileService.UpdateProfileAsync(adminId, request);
            
            if (!result) 
                return BadRequest(new { message = "فشل تحديث البيانات" });
            
            _logger.LogInformation($"Profile updated successfully for admin: {adminId}");
            return Ok(new { message = "تم تحديث البيانات بنجاح" });
        }

        // POST: api/admin/profile/picture
        [HttpPost("picture")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadPicture([FromForm] UploadProfilePictureDto request)
        {
            if (request.ProfilePicture == null)
            {
                _logger.LogWarning("No profile picture provided");
                return BadRequest(new { message = "الصورة مطلوبة" });
            }

            var adminId = GetAdminId();
            _logger.LogInformation($"Uploading profile picture for admin: {adminId}, File: {request.ProfilePicture.FileName}");
            
            var pictureUrl = await _profileService.UploadProfilePictureAsync(adminId, request.ProfilePicture);
            
            if (pictureUrl == null) 
                return BadRequest(new { message = "فشل رفع الصورة" });
            
            _logger.LogInformation($"Profile picture uploaded successfully for admin: {adminId}");
            return Ok(new { pictureUrl });
        }

        // DELETE: api/admin/profile/picture
        [HttpDelete("picture")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemovePicture()
        {
            var adminId = GetAdminId();
            _logger.LogInformation($"Removing profile picture for admin: {adminId}");
            
            var result = await _profileService.RemoveProfilePictureAsync(adminId);
            
            if (!result) 
                return BadRequest(new { message = "فشل حذف الصورة" });
            
            _logger.LogInformation($"Profile picture removed successfully for admin: {adminId}");
            return Ok(new { message = "تم حذف الصورة بنجاح" });
        }
    }
}