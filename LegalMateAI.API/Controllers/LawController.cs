using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.Domain.Enums;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/laws")]
    public class LawController : ControllerBase
    {
        private readonly ILawService _lawService;
        private readonly ILogger<LawController> _logger;

        public LawController(ILawService lawService, ILogger<LawController> logger)
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

        // ========== للجميع ==========

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetLaws([FromQuery] LawCategory? category = null, [FromQuery] string? search = null)
        {
            var laws = await _lawService.GetLawsAsync(category, search);
            return Ok(laws);
        }

        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<IActionResult> SearchLaws([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "يرجى إدخال كلمة البحث" });

            var laws = await _lawService.SearchLawsAsync(q);
            return Ok(laws);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLaw(Guid id)
        {
            var law = await _lawService.GetLawByIdAsync(id);
            if (law == null)
                return NotFound(new { message = "القانون غير موجود" });
            return Ok(law);
        }

        // ✅ أكشنز جديدة لجلب أجزاء منفصلة
        [AllowAnonymous]
        [HttpGet("{id}/core")]
        public async Task<IActionResult> GetLawCoreInfo(Guid id)
        {
            var info = await _lawService.GetLawCoreInfoAsync(id);
            if (info == null) return NotFound(new { message = "القانون غير موجود" });
            return Ok(info);
        }

        [AllowAnonymous]
        [HttpGet("{id}/links")]
        public async Task<IActionResult> GetLawLinks(Guid id)
        {
            var links = await _lawService.GetLawFileLinksAsync(id);
            if (links == null) return NotFound(new { message = "القانون غير موجود" });
            return Ok(links);
        }

        [AllowAnonymous]
        [HttpGet("{id}/metrics")]
        public async Task<IActionResult> GetLawMetrics(Guid id)
        {
            var metrics = await _lawService.GetLawMetricsAsync(id);
            if (metrics == null) return NotFound(new { message = "القانون غير موجود" });
            return Ok(metrics);
        }

        [AllowAnonymous]
        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetLawContent(Guid id)
        {
            var content = await _lawService.GetLawContentAsync(id);
            if (content == null) return NotFound(new { message = "القانون غير موجود" });
            return Ok(content);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}/audit")]
        public async Task<IActionResult> GetLawAudit(Guid id)
        {
            var audit = await _lawService.GetLawAuditAsync(id);
            if (audit == null) return NotFound(new { message = "القانون غير موجود" });
            return Ok(audit);
        }

        [AllowAnonymous]
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadLaw(Guid id)
        {
            var law = await _lawService.GetLawByIdAsync(id);
            if (law == null)
                return NotFound(new { message = "القانون غير موجود" });

            var fileBytes = await _lawService.DownloadLawAsync(id);
            if (fileBytes != null)
                return File(fileBytes, "application/pdf", $"{law.CoreInfo.Name}.pdf");

            if (!string.IsNullOrEmpty(law.FileLinks.PdfFileUrl))
                return Redirect(law.FileLinks.PdfFileUrl);
            if (!string.IsNullOrEmpty(law.FileLinks.SourceUrl))
                return Redirect(law.FileLinks.SourceUrl);

            return NotFound(new { message = "الملف غير متوفر" });
        }

        [AllowAnonymous]
        [HttpGet("{id}/download-url")]
        public async Task<IActionResult> GetDownloadUrl(Guid id)
        {
            var law = await _lawService.GetLawByIdAsync(id);
            if (law == null)
                return NotFound(new { message = "القانون غير موجود" });

            var url = await _lawService.GetLawDownloadUrlAsync(id);
            if (string.IsNullOrEmpty(url))
                return NotFound(new { message = "رابط التحميل غير متوفر" });

            return Ok(new { downloadUrl = url });
        }

        [AllowAnonymous]
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _lawService.GetLawCategoriesAsync();
            return Ok(categories);
        }

        // ========== للمستخدمين المسجلين ==========

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

        // ========== للأدمن فقط ==========

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/pending")]
        public async Task<IActionResult> GetPendingLaws()
        {
            var laws = await _lawService.GetPendingLawsAsync();
            return Ok(laws);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/{id}/approve")]
        public async Task<IActionResult> ApproveLaw(Guid id)
        {
            var adminId = GetUserId() ?? Guid.Empty;
            var result = await _lawService.ApproveLawAsync(adminId, id);

            if (!result)
                return NotFound(new { message = "القانون غير موجود" });

            return Ok(new { success = true, message = "تمت الموافقة على القانون بنجاح" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/{id}/reject")]
        public async Task<IActionResult> RejectLaw(Guid id, [FromBody] RejectLawRequest request)
        {
            var adminId = GetUserId() ?? Guid.Empty;
            var result = await _lawService.RejectLawAsync(adminId, id, request.Reason);

            if (!result)
                return NotFound(new { message = "القانون غير موجود" });

            return Ok(new { success = true, message = "تم رفض وحذف القانون بنجاح" });
        }
    }

    public class RejectLawRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}