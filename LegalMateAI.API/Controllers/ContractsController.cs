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
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _contractService;
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(
            IContractService contractService,
            ILogger<ContractsController> logger)
        {
            _contractService = contractService;
            _logger = logger;
        }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");
            return claim != null ? Guid.Parse(claim.Value) : null;
        }

        private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        private bool IsAdmin() => GetUserRole() == "Admin";

        // ================================================================
        // 📋 قوالب العقود (Public)
        // ================================================================

        [AllowAnonymous]
        [HttpGet("templates")]
        [ProducesResponseType(typeof(List<ContractTemplateResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetContractTemplates(
            [FromQuery] ContractType? type = null, 
            [FromQuery] string? search = null)
        {
            var templates = await _contractService.GetContractTemplatesAsync(type, search);
            return Ok(templates);
        }

        [AllowAnonymous]
        [HttpGet("templates/{id:guid}")]
        [ProducesResponseType(typeof(ContractTemplateResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetContractTemplateById(Guid id)
        {
            var template = await _contractService.GetTemplateByIdAsync(id);
            if (template == null)
                return NotFound(new { message = "القالب غير موجود" });
            return Ok(template);
        }

        // ================================================================
        // 📊 تحليل القالب مع تجميع الحقول المتكررة (Public)
        // ================================================================

        [AllowAnonymous]
        [HttpGet("templates/{id:guid}/analyze")]
        [ProducesResponseType(typeof(TemplateAnalysisDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> AnalyzeTemplate(Guid id)
        {
            var analysis = await _contractService.AnalyzeTemplateFullAsync(id);
            return Ok(analysis);
        }

        // ================================================================
        // 📋 جلب الـ Placeholders المطلوبة للقالب (Public)
        // ================================================================

        [AllowAnonymous]
        [HttpGet("templates/{id:guid}/placeholders")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemplatePlaceholders(Guid id)
        {
            var placeholders = await _contractService.GetTemplatePlaceholdersAsync(id);
            
            if (placeholders == null || placeholders.Count == 0)
            {
                return Ok(new
                {
                    templateId = id,
                    placeholdersCount = 0,
                    placeholders = new List<string>(),
                    message = "لا توجد Placeholders في هذا القالب، أو القالب غير موجود"
                });
            }
            
            return Ok(new
            {
                templateId = id,
                placeholdersCount = placeholders.Count,
                placeholders = placeholders
            });
        }

        // ================================================================
        // 🔧 استخراج واستبدال الفراغات تلقائياً (Admin Only)
        // ================================================================

        [Authorize(Roles = "Admin")]
        [HttpPost("templates/{id:guid}/extract-empty")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExtractEmptySpaces(Guid id)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            if (!IsAdmin())
                return StatusCode(403, new { message = "غير مصرح لك بهذه العملية" });

            var placeholders = await _contractService.ExtractAndReplaceEmptySpacesAsync(id);
            return Ok(new
            {
                templateId = id,
                placeholdersCount = placeholders.Count,
                placeholders = placeholders,
                message = placeholders.Count > 0
                    ? "تم استخراج واستبدال الفراغات بنجاح"
                    : "لا توجد فراغات في هذا القالب"
            });
        }

        // ================================================================
        // 🔄 تحويل جميع ملفات .doc إلى .docx (Admin Only - مرة واحدة)
        // ================================================================

        [Authorize(Roles = "Admin")]
        [HttpPost("templates/convert-all-to-docx")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ConvertAllToDocx()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            if (!IsAdmin())
                return StatusCode(403, new { message = "غير مصرح لك بهذه العملية" });

            var converted = await _contractService.ConvertAllDocToDocxAsync();
            
            return Ok(new
            {
                message = $"تم تحويل {converted} ملف من .doc إلى .docx",
                convertedCount = converted,
                note = "الآن جميع القوالب بصيغة .docx وجاهزة للاستخدام"
            });
        }

        // ================================================================
        // 🔄 إضافة Placeholders تلقائياً لجميع القوالب (Admin Only)
        // ================================================================

        [Authorize(Roles = "Admin")]
        [HttpPost("templates/add-placeholders-to-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> AddPlaceholdersToAllTemplates()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            if (!IsAdmin())
                return StatusCode(403, new { message = "غير مصرح لك بهذه العملية" });

            var processedCount = await _contractService.AddPlaceholdersToAllTemplatesAsync();
            
            return Ok(new
            {
                message = "تمت معالجة القوالب بنجاح",
                processedCount = processedCount,
                note = "الآن جميع القوالب تحتوي على Placeholders بصيغة {{field_1}}, {{field_2}}, إلخ"
            });
        }

        // ================================================================
        // 📄 توليد العقود (Auth Required)
        // ================================================================

        [Authorize]
        [HttpPost("generate")]
        [ProducesResponseType(typeof(ContractResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GenerateContract([FromBody] GenerateContractRequest request)
        {
            var userId = GetUserId();
            if (!userId.HasValue) 
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var template = await _contractService.GetTemplateByIdAsync(request.TemplateId);
            if (template == null) 
                return BadRequest(new { message = "القالب غير موجود" });

            var result = await _contractService.GenerateContractFromTemplateAsync(userId.Value, request);
            if (result == null) 
                return BadRequest(new { message = "فشل توليد العقد" });

            return Ok(result);
        }

        // ================================================================
        // 📁 عقودي (Auth Required)
        // ================================================================

        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(List<ContractResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyContracts(
            [FromQuery] string? search = null, 
            [FromQuery] ContractStatus? status = null)
        {
            var userId = GetUserId();
            if (!userId.HasValue) 
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var contracts = await _contractService.GetUserContractsAsync(userId.Value, status?.ToString(), search);
            return Ok(contracts);
        }

        // ================================================================
        // 🔍 بحث عام (Public)
        // ================================================================

        [AllowAnonymous]
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<ContractResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchContracts([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q)) 
                return BadRequest(new { message = "يرجى إدخال كلمة البحث" });

            var contracts = await _contractService.SearchAllContractsAsync(q);
            return Ok(contracts);
        }

        // ================================================================
        // 👀 عرض أي عقد (Public)
        // ================================================================

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ContractResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetContractById(Guid id)
        {
            var contract = await _contractService.GetAnyContractByIdAsync(id);
            if (contract == null)
                return NotFound(new { message = "العقد غير موجود" });
            return Ok(contract);
        }

        // ================================================================
        // ✏️ تعديل عقدي فقط (Auth Required - مالك العقد فقط)
        // ================================================================

        [Authorize]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ContractResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateContract(Guid id, [FromBody] UpdateContractDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue) 
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _contractService.UpdateContractAsync(userId.Value, id, request);
            if (result == null)
                return NotFound(new { message = "العقد غير موجود أو غير مصرح لك بتعديله" });

            return Ok(result);
        }

        [Authorize]
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateContractStatus(Guid id, [FromBody] UpdateContractStatusDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue) 
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _contractService.UpdateContractStatusAsync(userId.Value, id, request);
            if (!result)
                return NotFound(new { message = "العقد غير موجود أو غير مصرح لك" });

            return Ok(new { message = "تم تحديث حالة العقد بنجاح" });
        }

        // ================================================================
        // 🗑️ حذف عقدي فقط (Auth Required - مالك العقد فقط)
        // ================================================================

        [Authorize]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteContract(Guid id)
        {
            var userId = GetUserId();
            if (!userId.HasValue) 
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _contractService.DeleteContractAsync(userId.Value, id);
            if (!result)
                return NotFound(new { message = "العقد غير موجود أو غير مصرح لك بحذفه" });

            return Ok(new { message = "تم حذف العقد بنجاح" });
        }

        // ================================================================
        // 📥 تحميل أي عقد (Public)
        // ================================================================

        [AllowAnonymous]
        [HttpGet("{id:guid}/download")]
        public async Task<IActionResult> DownloadContract(Guid id)
        {
            var fileBytes = await _contractService.DownloadAnyContractAsync(id);
            if (fileBytes == null) 
                return NotFound(new { message = "العقد غير موجود" });

            var contract = await _contractService.GetAnyContractByIdAsync(id);
            var fileName = $"{(contract?.Title ?? "contract")}_{DateTime.Now:yyyyMMdd}.docx";
            
            return File(
                fileBytes, 
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName
            );
        }
    }
}