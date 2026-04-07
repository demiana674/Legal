// LegalMateAI.API/Controllers/CasesController.cs
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

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");
            
            if (claim == null)
            {
                throw new UnauthorizedAccessException("لم يتم العثور على معرف المستخدم");
            }
            
            return Guid.Parse(claim.Value);
        }

        private bool IsLawyer()
        {
            return User.IsInRole("Lawyer");
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        // ========== CRUD للقضايا ==========

        /// <summary>
        /// إنشاء قضية جديدة
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CaseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCase([FromBody] CreateCaseDto request)
        {
            var userId = GetUserId();
            var isLawyer = IsLawyer();

            var result = await _caseService.CreateCaseAsync(userId, request, isLawyer);

            if (result == null)
                return BadRequest(new { message = "فشل إنشاء القضية" });

            return Ok(result);
        }

        /// <summary>
        /// تحديث قضية
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(CaseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCase(Guid id, [FromBody] UpdateCaseDto request)
        {
            var userId = GetUserId();
            var isLawyer = IsLawyer();

            var result = await _caseService.UpdateCaseAsync(userId, id, request, isLawyer);

            if (result == null)
                return NotFound(new { message = "القضية غير موجودة أو غير مصرح لك بتعديلها" });

            return Ok(result);
        }

        /// <summary>
        /// حذف قضية
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCase(Guid id)
        {
            var userId = GetUserId();
            var isLawyer = IsLawyer();

            var result = await _caseService.DeleteCaseAsync(userId, id, isLawyer);

            if (!result)
                return NotFound(new { message = "القضية غير موجودة أو لا يمكن حذفها" });

            return Ok(new { message = "تم حذف القضية بنجاح" });
        }

        /// <summary>
        /// الحصول على قائمة القضايا (مع فلترة)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CaseResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCases([FromQuery] CaseFilterDto? filter)
        {
            var userId = GetUserId();
            var isLawyer = IsLawyer();

            filter ??= new CaseFilterDto();

            // إذا كان المستخدم محامي، يعرض قضاياه فقط
            if (isLawyer && !filter.LawyerId.HasValue && !filter.ClientId.HasValue)
            {
                filter.LawyerId = userId;
            }
            // إذا كان المستخدم عادي، يعرض قضاياه فقط
            else if (!isLawyer && !filter.ClientId.HasValue && !filter.LawyerId.HasValue)
            {
                filter.ClientId = userId;
            }

            var cases = await _caseService.GetCasesAsync(filter);
            return Ok(cases);
        }

        /// <summary>
        /// الحصول على قضية محددة
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CaseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCaseById(Guid id)
        {
            var userId = GetUserId();
            var isLawyer = IsLawyer();

            var caseEntity = await _caseService.GetCaseByIdAsync(userId, id, isLawyer);

            if (caseEntity == null)
                return NotFound(new { message = "القضية غير موجودة أو غير مصرح لك بمشاهدتها" });

            return Ok(caseEntity);
        }

        // ========== إدارة المستندات ==========

        /// <summary>
        /// رفع مستند للقضية
        /// </summary>
        [HttpPost("documents")]
        [ProducesResponseType(typeof(CaseDocumentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadDocument([FromForm] CreateCaseDocumentDto request)
        {
            var userId = GetUserId();

            var result = await _caseService.UploadDocumentAsync(userId, request);

            if (result == null)
                return BadRequest(new { message = "فشل رفع المستند" });

            return Ok(result);
        }

        /// <summary>
        /// حذف مستند
        /// </summary>
        [HttpDelete("documents/{documentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            var userId = GetUserId();

            var result = await _caseService.DeleteDocumentAsync(userId, documentId);

            if (!result)
                return NotFound(new { message = "المستند غير موجود أو لا يمكن حذفه" });

            return Ok(new { message = "تم حذف المستند بنجاح" });
        }

        /// <summary>
        /// تحميل مستند
        /// </summary>
        [HttpGet("documents/{documentId}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadDocument(Guid documentId)
        {
            var userId = GetUserId();

            var file = await _caseService.DownloadDocumentAsync(userId, documentId);

            if (file == null)
                return NotFound(new { message = "المستند غير موجود" });

            // ✅ الحصول على معلومات المستند لعرض الاسم الصحيح
            var document = await _caseService.GetDocumentByIdAsync(documentId);
            
            return File(file, "application/octet-stream", document?.FileName ?? "document");
        }

        /// <summary>
        /// عرض معلومات المستند
        /// </summary>
        [HttpGet("documents/{documentId}")]
        [ProducesResponseType(typeof(CaseDocumentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDocument(Guid documentId)
        {
            var userId = GetUserId();

            var document = await _caseService.GetDocumentByIdAsync(documentId);

            if (document == null)
                return NotFound(new { message = "المستند غير موجود" });

            return Ok(document);
        }

        // ========== إدارة الملاحظات ==========

        /// <summary>
        /// إضافة ملاحظة للقضية
        /// </summary>
        [HttpPost("notes")]
        [ProducesResponseType(typeof(CaseNoteResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddNote([FromBody] CreateCaseNoteDto request)
        {
            var userId = GetUserId();
            var isLawyer = IsLawyer();

            var result = await _caseService.AddNoteAsync(userId, request, isLawyer);

            if (result == null)
                return BadRequest(new { message = "فشل إضافة الملاحظة" });

            return Ok(result);
        }

        /// <summary>
        /// تحديث ملاحظة
        /// </summary>
        [HttpPut("notes/{noteId}")]
        [ProducesResponseType(typeof(CaseNoteResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNote(Guid noteId, [FromBody] string content)
        {
            var userId = GetUserId();

            var result = await _caseService.UpdateNoteAsync(userId, noteId, content);

            if (result == null)
                return NotFound(new { message = "الملاحظة غير موجودة أو غير مصرح لك بتعديلها" });

            return Ok(result);
        }

        /// <summary>
        /// حذف ملاحظة
        /// </summary>
        [HttpDelete("notes/{noteId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteNote(Guid noteId)
        {
            var userId = GetUserId();

            var result = await _caseService.DeleteNoteAsync(userId, noteId);

            if (!result)
                return NotFound(new { message = "الملاحظة غير موجودة أو لا يمكن حذفها" });

            return Ok(new { message = "تم حذف الملاحظة بنجاح" });
        }

        // ========== إحصائيات ==========

        /// <summary>
        /// الحصول على إحصائيات القضايا
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(CaseStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCaseStats()
        {
            var userId = GetUserId();
            var isLawyer = IsLawyer();

            CaseStatsDto stats;

            if (isLawyer)
            {
                stats = await _caseService.GetCaseStatsAsync(lawyerId: userId);
            }
            else
            {
                stats = await _caseService.GetCaseStatsAsync(clientId: userId);
            }

            return Ok(stats);
        }
    }
}