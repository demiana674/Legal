// LegalMateAI.API/Controllers/LawController.cs
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

        // ========== للجميع (Guest والمستخدمين) ==========
        
        /// <summary>
        /// جلب القوانين المتاحة للجميع (مع فلترة وتصنيف)
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(List<LawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLaws([FromQuery] LawCategory? category = null, [FromQuery] string? search = null)
        {
            var laws = await _lawService.GetLawsAsync(category, search);
            return Ok(laws);
        }

        /// <summary>
        /// البحث في القوانين
        /// </summary>
        [AllowAnonymous]
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<LawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchLaws([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "يرجى إدخال كلمة البحث" });
                
            var laws = await _lawService.SearchLawsAsync(q);
            return Ok(laws);
        }

        /// <summary>
        /// جلب قانون محدد
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LawDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLaw(Guid id)
        {
            var law = await _lawService.GetLawByIdAsync(id);
            if (law == null)
                return NotFound(new { message = "القانون غير موجود" });
            return Ok(law);
        }

        /// <summary>
        /// تحميل القانون (يحمل الملف أو يحول على الرابط الخارجي)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadLaw(Guid id)
        {
            // جرب نرجع الملف مباشرة لو الرابط متاح
            var fileBytes = await _lawService.DownloadLawAsync(id);
            if (fileBytes != null && fileBytes.Length > 0)
            {
                var law = await _lawService.GetLawByIdAsync(id);
                var fileName = law?.Name ?? "law";
                return File(fileBytes, "application/pdf", $"{fileName}.pdf");
            }

            // لو مفيش ملف، نحول على الرابط الخارجي
            var downloadUrl = await _lawService.GetLawDownloadUrlAsync(id);
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                return Redirect(downloadUrl);
            }

            return NotFound(new { message = "الملف غير متوفر" });
        }

        /// <summary>
        /// الحصول على رابط تحميل القانون
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}/download-url")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDownloadUrl(Guid id)
        {
            var url = await _lawService.GetLawDownloadUrlAsync(id);
            if (string.IsNullOrEmpty(url))
                return NotFound(new { message = "رابط التحميل غير متوفر" });

            return Ok(new { downloadUrl = url });
        }

        /// <summary>
        /// جلب تصنيفات القوانين
        /// </summary>
        [AllowAnonymous]
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<LawCategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _lawService.GetLawCategoriesAsync();
            return Ok(categories);
        }

        // ========== للمستخدمين المسجلين ==========
        
        /// <summary>
        /// رفع قانون جديد مع بارسر آلي للرابط
        /// </summary>
        [Authorize]
        [HttpPost("upload-with-parser")]
        [ProducesResponseType(typeof(LawDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadLawWithParser([FromBody] CreateLawRequestDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            if (string.IsNullOrEmpty(request.SourceUrl))
                return BadRequest(new { message = "رابط المصدر مطلوب" });

            var result = await _lawService.UploadLawByUserWithParserAsync(userId.Value, request);
            
            if (result == null)
                return BadRequest(new { message = "فشل رفع القانون. تأكد من صحة الرابط." });

            _logger.LogInformation($"Law uploaded by user {userId}: {result.Name}");

            return Ok(new
            {
                success = true,
                message = "تم رفع القانون بنجاح. في انتظار موافقة الأدمن.",
                law = result,
                isPendingApproval = true
            });
        }

        /// <summary>
        /// رفع قانون بالطريقة التقليدية (ملف PDF)
        /// </summary>
        [Authorize]
        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadLawByUser([FromForm] AddLawDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });
                
            var result = await _lawService.UploadLawByUserAsync(userId.Value, request);
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
        /// جلب القوانين اللي رفعها المستخدم الحالي
        /// </summary>
        [Authorize]
        [HttpGet("my-uploads")]
        [ProducesResponseType(typeof(List<LawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyUploads()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });
                
            var laws = await _lawService.GetUserUploadedLawsAsync(userId.Value);
            return Ok(laws);
        }

        // ========== للأدمن فقط ==========
        
        /// <summary>
        /// إضافة قانون جديد (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddLaw([FromForm] AddLawDto request)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول كأدمن" });
                
            var result = await _lawService.AddLawAsync(
                adminId.Value, request.PdfFile, request.Name, request.Category,
                request.LawNumber, request.Year, request.Description,
                request.SourceUrl, request.SearchKeywords);
                
            if (result == null)
                return BadRequest(new { message = "فشل إضافة القانون" });
                
            _logger.LogInformation($"Law added by admin {adminId}: {result.Name}");
            return Ok(new { success = true, message = "تم إضافة القانون بنجاح", law = result });
        }

        /// <summary>
        /// تحديث قانون (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(LawDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLaw(Guid id, [FromBody] UpdateLawDto request)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول كأدمن" });
                
            var result = await _lawService.UpdateLawAsync(adminId.Value, id, request);
            if (result == null)
                return NotFound(new { message = "القانون غير موجود" });
                
            return Ok(new { success = true, message = "تم تحديث القانون بنجاح", law = result });
        }

        /// <summary>
        /// حذف قانون (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteLaw(Guid id)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول كأدمن" });
                
            var result = await _lawService.DeleteLawAsync(adminId.Value, id);
            if (!result)
                return NotFound(new { message = "القانون غير موجود" });
                
            return Ok(new { success = true, message = "تم حذف القانون بنجاح" });
        }

        /// <summary>
        /// جلب كل القوانين للأدمن (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/all")]
        [ProducesResponseType(typeof(List<LawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllLawsForAdmin()
        {
            var laws = await _lawService.GetAllLawsForAdminAsync();
            return Ok(laws);
        }

        /// <summary>
        /// جلب القوانين المنتظرة للموافقة (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/pending")]
        [ProducesResponseType(typeof(List<LawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingLaws()
        {
            var laws = await _lawService.GetPendingLawsAsync();
            return Ok(laws);
        }

        /// <summary>
        /// جلب القوانين اللي رفعها المستخدمين ولسه متوافقش عليها (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/pending-user-laws")]
        [ProducesResponseType(typeof(List<LawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingUserLaws()
        {
            var laws = await _lawService.GetPendingLawsForAdminAsync();
            return Ok(laws);
        }

        /// <summary>
        /// الموافقة على قانون (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/{id}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveLaw(Guid id)
        {
            var adminId = GetUserId() ?? Guid.Empty;
            var result = await _lawService.ApproveLawAsync(adminId, id);
            
            if (!result)
                return NotFound(new { message = "القانون غير موجود" });
                
            return Ok(new { success = true, message = "تمت الموافقة على القانون بنجاح" });
        }

        /// <summary>
        /// الموافقة على قانون من مستخدم (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/approve-user-law/{lawId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveUserLaw(Guid lawId)
        {
            var adminId = GetUserId() ?? Guid.Empty;
            var result = await _lawService.ApproveUserLawAsync(adminId, lawId);
            
            if (!result)
                return NotFound(new { message = "القانون غير موجود" });

            _logger.LogInformation($"User law {lawId} approved by admin {adminId}");
            return Ok(new { success = true, message = "تمت الموافقة على القانون بنجاح" });
        }

        /// <summary>
        /// رفض قانون (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/{id}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectLaw(Guid id, [FromBody] RejectLawRequest request)
        {
            var adminId = GetUserId() ?? Guid.Empty;
            var result = await _lawService.RejectLawAsync(adminId, id, request.Reason);
            
            if (!result)
                return NotFound(new { message = "القانون غير موجود" });
                
            return Ok(new { success = true, message = "تم رفض القانون" });
        }

        /// <summary>
        /// رفض قانون من مستخدم (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/reject-user-law/{lawId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectUserLaw(Guid lawId, [FromBody] RejectLawRequest request)
        {
            var adminId = GetUserId() ?? Guid.Empty;
            var result = await _lawService.RejectUserLawAsync(adminId, lawId, request.Reason);
            
            if (!result)
                return NotFound(new { message = "القانون غير موجود" });

            _logger.LogInformation($"User law {lawId} rejected by admin {adminId}: {request.Reason}");
            return Ok(new { success = true, message = "تم رفض القانون" });
        }
    }

    /// <summary>
    /// نموذج رفض القانون
    /// </summary>
    public class RejectLawRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}