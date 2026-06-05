using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.DTOs.CreateDTO;
using System.Security.Claims;
using LegalMateAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;

namespace LegalMateAI.API.Controllers
{
    [Authorize(Roles = "Lawyer")]
    [ApiController]
    [Route("api/lawyer/profile")]
    public class LawyerProfileController : ControllerBase
    {
        private readonly ILawyerProfileService _profileService;
        private readonly ILogService _logService;
        private readonly LegalMateDbContext _context;

        public LawyerProfileController(
            ILawyerProfileService profileService, 
            ILogService logService,
            LegalMateDbContext context)
        {
            _profileService = profileService;
            _logService = logService;
            _context = context;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("id")
                ?? User.FindFirst("sub");
            return Guid.Parse(claim!.Value);
        }

        /// <summary>
        /// الحصول على الملف الشخصي للمحامي
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var profile = await _profileService.GetProfileAsync(GetUserId());
            if (profile == null)
                return NotFound(new { message = "الملف الشخصي غير موجود" });
            return Ok(profile);
        }

        /// <summary>
        /// الحصول على لوحة التحكم (Dashboard)
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var dashboard = await _profileService.GetDashboardAsync(GetUserId());
            return Ok(dashboard);
        }

        /// <summary>
        /// تحديث الملف الشخصي بالكامل
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateLawyerProfileDto request)
        {
            var userId = GetUserId();
            var result = await _profileService.UpdateProfileAsync(userId, request);
            if (!result) 
                return BadRequest(new { message = "فشل تحديث البيانات" });

            await _logService.LogActionAsync(userId, AdminLogAction.UpdateProfile, "Lawyer", userId);
            return Ok(new { message = "تم تحديث البيانات بنجاح" });
        }

        /// <summary>
        /// رفع صورة شخصية
        /// </summary>
        [HttpPost("picture")]
        public async Task<IActionResult> UploadPicture([FromForm] UploadProfilePictureDto request)
        {
            var userId = GetUserId();
            var pictureUrl = await _profileService.UploadProfilePictureAsync(userId, request.ProfilePicture);
            if (pictureUrl == null) 
                return BadRequest(new { message = "فشل رفع الصورة" });

            await _logService.LogActionAsync(userId, AdminLogAction.UpdateProfile, "Lawyer", userId);
            return Ok(new { pictureUrl });
        }

        /// <summary>
        /// حذف الصورة الشخصية
        /// </summary>
        [HttpDelete("picture")]
        public async Task<IActionResult> RemovePicture()
        {
            var userId = GetUserId();
            var result = await _profileService.RemoveProfilePictureAsync(userId);
            if (!result) 
                return BadRequest(new { message = "فشل حذف الصورة" });

            await _logService.LogActionAsync(userId, AdminLogAction.UpdateProfile, "Lawyer", userId);
            return Ok(new { message = "تم حذف الصورة بنجاح" });
        }

        /// <summary>
        /// ✅ تحديث مهارات المحامي فقط
        /// </summary>
        [HttpPut("skills")]
        public async Task<IActionResult> UpdateSkills([FromBody] UpdateSkillsDto request)
        {
            var userId = GetUserId();
            
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null)
                return NotFound(new { message = "الملف الشخصي غير موجود" });

            if (request.Skills != null && request.Skills.Any())
            {
                user.LawyerProfile.Skills = string.Join(",", request.Skills);
                user.LawyerProfile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else if (!string.IsNullOrEmpty(request.SkillsText))
            {
                user.LawyerProfile.Skills = request.SkillsText;
                user.LawyerProfile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await _logService.LogActionAsync(userId, AdminLogAction.UpdateProfile, "Lawyer", userId);
            
            // إرجاع المهارات المحدثة
            var updatedSkills = string.IsNullOrEmpty(user.LawyerProfile.Skills)
                ? new List<string>()
                : user.LawyerProfile.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

            return Ok(new 
            { 
                message = "تم تحديث المهارات بنجاح", 
                skills = updatedSkills 
            });
        }

        /// <summary>
        /// ✅ الحصول على مهارات المحامي فقط
        /// </summary>
        [HttpGet("skills")]
        public async Task<IActionResult> GetSkills()
        {
            var userId = GetUserId();
            
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null)
                return NotFound(new { message = "الملف الشخصي غير موجود" });

            var skills = string.IsNullOrEmpty(user.LawyerProfile.Skills)
                ? new List<string>()
                : user.LawyerProfile.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

            return Ok(new { skills });
        }

        /// <summary>
        /// ✅ تحديث عدد المحاج المعتمدة (للاستخدام الداخلي أو للأدمن)
        /// </summary>
        [HttpPut("approved-litigations")]
        public async Task<IActionResult> UpdateApprovedLitigations([FromBody] UpdateApprovedLitigationsDto request)
        {
            var userId = GetUserId();
            
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null)
                return NotFound(new { message = "الملف الشخصي غير موجود" });

            if (request.Count.HasValue)
            {
                user.LawyerProfile.ApprovedLitigationsCount = request.Count.Value;
                user.LawyerProfile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await _logService.LogActionAsync(userId, AdminLogAction.UpdateProfile, "Lawyer", userId);
            
            return Ok(new 
            { 
                message = "تم تحديث عدد المحاج المعتمدة بنجاح", 
                approvedLitigationsCount = user.LawyerProfile.ApprovedLitigationsCount 
            });
        }

        /// <summary>
        /// ✅ الحصول على إحصائيات المحامي (للعرض في الصفحة الشخصية)
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetLawyerStats()
        {
            var userId = GetUserId();
            
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null)
                return NotFound(new { message = "الملف الشخصي غير موجود" });

            var lawyerProfile = user.LawyerProfile;

            // حساب الإحصائيات من قاعدة البيانات
            var activeCases = await _context.Cases
                .CountAsync(c => c.LawyerId == lawyerProfile.Id && 
                    c.Status != CaseStatus.Completed && 
                    c.Status != CaseStatus.Rejected);

            var totalClients = await _context.Cases
                .Where(c => c.LawyerId == lawyerProfile.Id)
                .Select(c => c.ClientId)
                .Distinct()
                .CountAsync();

            var totalReviews = await _context.LawyerReviews
                .CountAsync(r => r.LawyerId == lawyerProfile.Id);

            var avgRating = await _context.LawyerReviews
                .Where(r => r.LawyerId == lawyerProfile.Id)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            return Ok(new
            {
                approvedLitigationsCount = lawyerProfile.ApprovedLitigationsCount,
                clientsCount = totalClients,
                activeCasesCount = activeCases,
                totalReviews = totalReviews,
                averageRating = Math.Round(avgRating, 1),
                totalCases = await _context.Cases.CountAsync(c => c.LawyerId == lawyerProfile.Id),
                totalAppointments = await _context.Appointments.CountAsync(a => a.LawyerId == lawyerProfile.Id)
            });
        }
    }

    /// <summary>
    /// DTO لتحديث المهارات
    /// </summary>
    public class UpdateSkillsDto
    {
        public List<string> Skills { get; set; } = new List<string>();
        public string? SkillsText { get; set; }
    }

    /// <summary>
    /// DTO لتحديث عدد المحاج المعتمدة
    /// </summary>
    public class UpdateApprovedLitigationsDto
    {
        public int? Count { get; set; }
    }
}