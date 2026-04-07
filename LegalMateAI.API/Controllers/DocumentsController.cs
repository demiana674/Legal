using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentAnalysisService _analysisService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentService documentService,
            IDocumentAnalysisService analysisService,
            ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _analysisService = analysisService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }

        /// <summary>
        /// رفع مستند جديد
        /// </summary>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(ApiResponse<DocumentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadDocument([FromForm] CreateDocumentDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(ApiResponse<object>.BadRequest("الملف مطلوب"));
            }

            var result = await _documentService.UploadDocumentAsync(userId, request);
            
            if (result == null)
            {
                return BadRequest(ApiResponse<object>.BadRequest("فشل رفع الملف"));
            }

            _logger.LogInformation("Document uploaded: {DocumentId} by user {UserId}", result.Id, userId);
            return Ok(ApiResponse<DocumentResponseDto>.Ok(result, "تم رفع المستند بنجاح"));
        }

        /// <summary>
        /// الحصول على جميع مستندات المستخدم
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<DocumentResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            var documents = await _documentService.GetUserDocumentsAsync(userId, page, pageSize);
            return Ok(ApiResponse<List<DocumentResponseDto>>.Ok(documents));
        }

        /// <summary>
        /// الحصول على مستند محدد مع التحقق من الصلاحية (Resource-based Authorization)
        /// </summary>
        [HttpGet("{documentId}")]
        [ProducesResponseType(typeof(ApiResponse<DocumentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDocumentById(Guid documentId)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            var document = await _documentService.GetDocumentByIdAsync(userId, documentId);
            
            if (document == null)
            {
                return NotFound(ApiResponse<object>.NotFound("المستند غير موجود"));
            }

            // ✅ Resource-based Authorization: التحقق من أن المستخدم هو صاحب المستند
            if (document.UserId != userId && userRole != "Admin")
            {
                _logger.LogWarning("Unauthorized access: User {UserId} tried to access document {DocumentId} owned by {OwnerId}", 
                    userId, documentId, document.UserId);
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Unauthorized("غير مصرح لك بمشاهدة هذا المستند"));
            }

            return Ok(ApiResponse<DocumentResponseDto>.Ok(document));
        }

        /// <summary>
        /// الحصول على تحليل المستند مع التحقق من الصلاحية
        /// </summary>
        [HttpGet("{documentId}/analysis")]
        [ProducesResponseType(typeof(ApiResponse<DocumentAnalysisResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDocumentAnalysis(Guid documentId)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            // ✅ التحقق من وجود المستند أولاً
            var document = await _documentService.GetDocumentByIdAsync(userId, documentId);
            if (document == null)
            {
                return NotFound(ApiResponse<object>.NotFound("المستند غير موجود"));
            }

            // ✅ Resource-based Authorization
            if (document.UserId != userId && userRole != "Admin")
            {
                _logger.LogWarning("Unauthorized access: User {UserId} tried to view analysis of document {DocumentId} owned by {OwnerId}", 
                    userId, documentId, document.UserId);
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Unauthorized("غير مصرح لك بمشاهدة تحليل هذا المستند"));
            }

            var analysis = await _analysisService.GetAnalysisByDocumentAsync(userId, documentId);
            
            if (analysis == null)
            {
                return NotFound(ApiResponse<object>.NotFound("التحليل غير موجود"));
            }

            return Ok(ApiResponse<DocumentAnalysisResponseDto>.Ok(analysis));
        }

        /// <summary>
        /// تحليل مستند باستخدام الذكاء الاصطناعي مع التحقق من الصلاحية
        /// </summary>
        [HttpPost("{documentId}/analyze")]
        [ProducesResponseType(typeof(ApiResponse<DocumentAnalysisResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AnalyzeDocument(
            Guid documentId, 
            [FromBody] CreateDocumentAnalysisDto request)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            // ✅ التحقق من وجود المستند أولاً
            var document = await _documentService.GetDocumentByIdAsync(userId, documentId);
            if (document == null)
            {
                return NotFound(ApiResponse<object>.NotFound("المستند غير موجود"));
            }

            // ✅ Resource-based Authorization
            if (document.UserId != userId && userRole != "Admin")
            {
                _logger.LogWarning("Unauthorized analysis: User {UserId} tried to analyze document {DocumentId} owned by {OwnerId}", 
                    userId, documentId, document.UserId);
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Unauthorized("غير مصرح لك بتحليل هذا المستند"));
            }

            var result = await _analysisService.AnalyzeDocumentAsync(userId, documentId, request);
            
            if (result == null)
            {
                return BadRequest(ApiResponse<object>.BadRequest("فشل تحليل المستند"));
            }

            _logger.LogInformation("Document analyzed: {DocumentId} by user {UserId}", documentId, userId);
            return Ok(ApiResponse<DocumentAnalysisResponseDto>.Ok(result, "تم تحليل المستند بنجاح"));
        }

        /// <summary>
        /// حذف مستند مع التحقق من الصلاحية
        /// </summary>
        [HttpDelete("{documentId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            // ✅ التحقق من وجود المستند أولاً
            var document = await _documentService.GetDocumentByIdAsync(userId, documentId);
            if (document == null)
            {
                return NotFound(ApiResponse<object>.NotFound("المستند غير موجود"));
            }

            // ✅ Resource-based Authorization
            if (document.UserId != userId && userRole != "Admin")
            {
                _logger.LogWarning("Unauthorized delete: User {UserId} tried to delete document {DocumentId} owned by {OwnerId}", 
                    userId, documentId, document.UserId);
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Unauthorized("غير مصرح لك بحذف هذا المستند"));
            }

            var result = await _documentService.DeleteDocumentAsync(userId, documentId);
            
            if (!result)
            {
                return NotFound(ApiResponse<object>.NotFound("المستند غير موجود"));
            }

            _logger.LogInformation("Document deleted: {DocumentId} by user {UserId}", documentId, userId);
            return Ok(ApiResponse<object>.Ok("تم حذف المستند بنجاح"));
        }
    }
}