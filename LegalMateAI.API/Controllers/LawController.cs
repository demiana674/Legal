using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LawsController : ControllerBase
    {
        private readonly ILawService _lawService;
        private readonly ILogger<LawsController> _logger;

        public LawsController(ILawService lawService, ILogger<LawsController> logger)
        {
            _lawService = lawService;
            _logger = logger;
        }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");
            return claim != null ? Guid.Parse(claim.Value) : null;
        }

        // ==================== للجميع (3 Endpoints فقط) ====================

        /// <summary>
        /// جلب جميع القوانين (مع فلترة حسب التصنيف أو البحث)
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(List<LawCoreInfoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllLaws(
            [FromQuery] LawCategory? category = null,
            [FromQuery] string? search = null)
        {
            var laws = await _lawService.GetAllLawsAsync(category, search);
            return Ok(laws);
        }

        /// <summary>
        /// البحث في القوانين
        /// </summary>
        [AllowAnonymous]
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<LawCoreInfoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchLaws([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "يرجى إدخال كلمة البحث" });

            var laws = await _lawService.SearchLawsAsync(q);
            return Ok(laws);
        }

        /// <summary>
        /// جلب قانون محدد بالـ ID
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(LawCoreInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLawById(Guid id)
        {
            var law = await _lawService.GetLawByIdAsync(id);
            if (law == null)
                return NotFound(new { message = "القانون غير موجود" });
            return Ok(law);
        }

        /// <summary>
        /// تحميل ملف القانون
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id:guid}/download")]
        public async Task<IActionResult> DownloadLaw(Guid id)
        {
            var law = await _lawService.GetLawByIdAsync(id);
            if (law == null)
                return NotFound(new { message = "القانون غير موجود" });

            var fileBytes = await _lawService.DownloadLawAsync(id);
            if (fileBytes != null)
                return File(fileBytes, "application/pdf", $"{law.Name}.pdf");

            var downloadUrl = await _lawService.GetLawDownloadUrlAsync(id);
            if (!string.IsNullOrEmpty(downloadUrl))
                return Redirect(downloadUrl);

            return NotFound(new { message = "الملف غير متوفر" });
        }

        /// <summary>
        /// جلب رابط تحميل القانون
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id:guid}/download-url")]
        public async Task<IActionResult> GetDownloadUrl(Guid id)
        {
            var url = await _lawService.GetLawDownloadUrlAsync(id);
            if (string.IsNullOrEmpty(url))
                return NotFound(new { message = "رابط التحميل غير متوفر" });
            return Ok(new { downloadUrl = url });
        }

        /// <summary>
        /// جلب تصنيفات القوانين مع الإحصائيات
        /// </summary>
        [AllowAnonymous]
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _lawService.GetLawCategoriesAsync();
            return Ok(categories);
        }

        // ==================== للمستخدمين المسجلين ====================

        /// <summary>
        /// رفع قانون جديد (يحتاج موافقة الأدمن)
        /// </summary>
        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadLaw([FromForm] AddLawDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _lawService.UploadLawByUserAsync(userId, request);
            if (result == null)
                return BadRequest(new { message = "فشل رفع القانون" });

            return Ok(new
            {
                success = true,
                message = "تم رفع القانون بنجاح، في انتظار موافقة الأدمن",
                law = result
            });
        }

        /// <summary>
        /// جلب القوانين التي رفعها المستخدم
        /// </summary>
        [Authorize]
        [HttpGet("my-uploads")]
        public async Task<IActionResult> GetMyUploads()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var laws = await _lawService.GetUserUploadedLawsAsync(userId.Value);
            return Ok(laws);
        }

        // ==================== للأدمن فقط (CRUD كامل) ====================

        /// <summary>
        /// إنشاء قانون جديد (مباشر بدون انتظار موافقة)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateLaw([FromForm] CreateLawDto request)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _lawService.CreateLawAsync(adminId.Value, request);
            if (result == null)
                return BadRequest(new { message = "فشل إنشاء القانون" });

            return Ok(new { success = true, message = "تم إنشاء القانون بنجاح", law = result });
        }

        /// <summary>
        /// تحديث قانون موجود
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateLaw(Guid id, [FromForm] UpdateLawDto request)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _lawService.UpdateLawAsync(adminId.Value, id, request);
            if (result == null)
                return NotFound(new { message = "القانون غير موجود" });

            return Ok(new { success = true, message = "تم تحديث القانون بنجاح", law = result });
        }

        /// <summary>
        /// حذف قانون
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteLaw(Guid id)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _lawService.DeleteLawAsync(adminId.Value, id);
            if (!result)
                return NotFound(new { message = "القانون غير موجود" });

            return Ok(new { success = true, message = "تم حذف القانون بنجاح" });
        }

        /// <summary>
        /// جلب القوانين المعلقة (التي تحتاج موافقة)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingLaws()
        {
            var laws = await _lawService.GetPendingLawsAsync();
            return Ok(laws);
        }

        /// <summary>
        /// الموافقة على قانون
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:guid}/approve")]
        public async Task<IActionResult> ApproveLaw(Guid id)
        {
            var adminId = GetUserId() ?? Guid.Empty;
            var result = await _lawService.ApproveLawAsync(adminId, id);

            if (!result)
                return NotFound(new { message = "القانون غير موجود" });

            return Ok(new { success = true, message = "تمت الموافقة على القانون بنجاح" });
        }

        /// <summary>
        /// رفض قانون (مع ذكر السبب)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:guid}/reject")]
        public async Task<IActionResult> RejectLaw(Guid id, [FromBody] RejectLawRequest request)
        {
            var adminId = GetUserId() ?? Guid.Empty;
            var result = await _lawService.RejectLawAsync(adminId, id, request.Reason);

            if (!result)
                return NotFound(new { message = "القانون غير موجود" });

            return Ok(new { success = true, message = "تم رفض القانون وحذفه بنجاح" });
        }
    }

    public class RejectLawRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}