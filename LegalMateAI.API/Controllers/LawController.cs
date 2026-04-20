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

        [AllowAnonymous]
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadLaw(Guid id)
        {
            var fileBytes = await _lawService.DownloadLawAsync(id);
            if (fileBytes == null)
                return NotFound(new { message = "الملف غير موجود" });
            
            var law = await _lawService.GetLawByIdAsync(id);
            var fileName = law?.Name ?? "law";
            return File(fileBytes, "application/pdf", $"{fileName}.pdf");
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
        public async Task<IActionResult> UploadLawByUser([FromForm] AddLawDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var result = await _lawService.UploadLawByUserAsync(userId.Value, request);
            if (result == null)
                return BadRequest(new { message = "فشل رفع القانون" });
                
            return Ok(new { message = "تم رفع القانون بنجاح، في انتظار موافقة الأدمن", law = result });
        }

        [Authorize]
        [HttpGet("my-uploads")]
        public async Task<IActionResult> GetMyUploads()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var laws = await _lawService.GetUserUploadedLawsAsync(userId.Value);
            return Ok(laws);
        }

        // ========== للأدمن فقط ==========
        
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddLaw([FromForm] AddLawDto request)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized();
                
            var result = await _lawService.AddLawAsync(
                adminId.Value, request.PdfFile, request.Name, request.Category,
                request.LawNumber, request.Year, request.Description,
                request.SourceUrl, request.SearchKeywords);
                
            if (result == null)
                return BadRequest(new { message = "فشل إضافة القانون" });
                
            return Ok(new { message = "تم إضافة القانون بنجاح", law = result });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLaw(Guid id, [FromBody] UpdateLawDto request)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized();
                
            var result = await _lawService.UpdateLawAsync(adminId.Value, id, request);
            if (result == null)
                return NotFound(new { message = "القانون غير موجود" });
                
            return Ok(new { message = "تم تحديث القانون بنجاح", law = result });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLaw(Guid id)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized();
                
            var result = await _lawService.DeleteLawAsync(adminId.Value, id);
            if (!result)
                return NotFound(new { message = "القانون غير موجود" });
                
            return Ok(new { message = "تم حذف القانون بنجاح" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllLawsForAdmin()
        {
            var laws = await _lawService.GetAllLawsForAdminAsync();
            return Ok(laws);
        }

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
                
            return Ok(new { message = "تمت الموافقة على القانون بنجاح" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/{id}/reject")]
        public async Task<IActionResult> RejectLaw(Guid id, [FromBody] RejectLawRequest request)
        {
            var adminId = GetUserId() ?? Guid.Empty;
            var result = await _lawService.RejectLawAsync(adminId, id, request.Reason);
            
            if (!result)
                return NotFound(new { message = "القانون غير موجود" });
                
            return Ok(new { message = "تم رفض القانون" });
        }
    }

    public class RejectLawRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}