using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CasesController : ControllerBase
    {
        private readonly ICaseService _caseService;
        private readonly ILogger<CasesController> _logger;

        public CasesController(ICaseService caseService, ILogger<CasesController> logger)
        {
            _caseService = caseService;
            _logger = logger;
        }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");
            return claim != null ? Guid.Parse(claim.Value) : null;
        }

        private bool IsLawyer() => User.IsInRole("Lawyer");
        private bool IsAdmin() => User.IsInRole("Admin");

        // ========== CRUD للقضايا ==========

        [HttpPost]
        [ProducesResponseType(typeof(CaseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCase([FromBody] CreateCaseDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var isLawyer = IsLawyer();
            var result = await _caseService.CreateCaseAsync(userId.Value, request, isLawyer);
            if (result == null)
                return BadRequest(new { message = "فشل إنشاء القضية" });
            return Ok(result);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(CaseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCase(Guid id, [FromBody] UpdateCaseDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var isLawyer = IsLawyer();
            var result = await _caseService.UpdateCaseAsync(userId.Value, id, request, isLawyer);
            if (result == null)
                return NotFound(new { message = "القضية غير موجودة أو غير مصرح لك بتعديلها" });
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCase(Guid id)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var isLawyer = IsLawyer();
            var result = await _caseService.DeleteCaseAsync(userId.Value, id, isLawyer);
            if (!result)
                return NotFound(new { message = "القضية غير موجودة أو لا يمكن حذفها" });
            return Ok(new { message = "تم حذف القضية بنجاح" });
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<CaseResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCases([FromQuery] CaseFilterDto? filter)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var isLawyer = IsLawyer();
            
            // ✅ استخدام الدالة الآمنة الجديدة
            var cases = await _caseService.GetCasesForUserAsync(userId.Value, filter ?? new CaseFilterDto(), isLawyer);
            return Ok(cases);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CaseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCaseById(Guid id)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var isLawyer = IsLawyer();
            var caseEntity = await _caseService.GetCaseByIdAsync(userId.Value, id, isLawyer);
            if (caseEntity == null)
                return NotFound(new { message = "القضية غير موجودة أو غير مصرح لك بمشاهدتها" });
            return Ok(caseEntity);
        }

        // ========== إدارة المستندات ==========

        [HttpPost("documents")]
        [ProducesResponseType(typeof(CaseDocumentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadDocument([FromForm] CreateCaseDocumentDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _caseService.UploadDocumentAsync(userId.Value, request);
            if (result == null)
                return BadRequest(new { message = "فشل رفع المستند" });
            return Ok(result);
        }

        [HttpDelete("documents/{documentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _caseService.DeleteDocumentAsync(userId.Value, documentId);
            if (!result)
                return NotFound(new { message = "المستند غير موجود أو لا يمكن حذفه" });
            return Ok(new { message = "تم حذف المستند بنجاح" });
        }

        [AllowAnonymous]
        [HttpGet("documents/{documentId}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadDocument(Guid documentId)
        {
            var file = await _caseService.DownloadDocumentAsync(documentId);
            if (file == null)
                return NotFound(new { message = "المستند غير موجود" });

            var document = await _caseService.GetDocumentByIdAsync(documentId);
            var contentType = GetContentType(document?.FileName ?? "document.pdf");
            return File(file, contentType, document?.FileName ?? "document");
        }

        // ========== إدارة الملاحظات ==========

        [HttpPost("notes")]
        [ProducesResponseType(typeof(CaseNoteResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddNote([FromBody] CreateCaseNoteDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var isLawyer = IsLawyer();
            var result = await _caseService.AddNoteAsync(userId.Value, request, isLawyer);
            if (result == null)
                return BadRequest(new { message = "فشل إضافة الملاحظة" });
            return Ok(result);
        }

        [HttpPut("notes/{noteId}")]
        [ProducesResponseType(typeof(CaseNoteResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNote(Guid noteId, [FromBody] string content)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _caseService.UpdateNoteAsync(userId.Value, noteId, content);
            if (result == null)
                return NotFound(new { message = "الملاحظة غير موجودة أو غير مصرح لك بتعديلها" });
            return Ok(result);
        }

        [HttpDelete("notes/{noteId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteNote(Guid noteId)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _caseService.DeleteNoteAsync(userId.Value, noteId);
            if (!result)
                return NotFound(new { message = "الملاحظة غير موجودة أو لا يمكن حذفها" });
            return Ok(new { message = "تم حذف الملاحظة بنجاح" });
        }

        // ========== إحصائيات ==========

        [HttpGet("stats")]
        [ProducesResponseType(typeof(CaseStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCaseStats()
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var isLawyer = IsLawyer();
            
            // ✅ استخدام الدالة الآمنة الجديدة
            var stats = await _caseService.GetCaseStatsForUserAsync(userId.Value, isLawyer);
            return Ok(stats);
        }

        // ========== Helper ==========

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}