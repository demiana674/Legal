using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;
using System.Security.Claims;
using Xceed.Words.NET; // ✅ مهم لقراءة ملفات Word

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _contractService;
        private readonly IWebHostEnvironment _env; // ✅ لإضافة البيئة
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(
            IContractService contractService,
            IWebHostEnvironment env,
            ILogger<ContractsController> logger)
        {
            _contractService = contractService;
            _env = env;
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
        // 📖 قراءة محتوى قالب Word (للعرض والتعبئة) - ✅ الـ Endpoint الجديد
        // ================================================================

        [AllowAnonymous]
        [HttpGet("templates/{id:guid}/read-content")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReadTemplateContent(Guid id)
        {
            var template = await _contractService.GetTemplateByIdAsync(id);
            if (template == null)
                return NotFound(new { message = "القالب غير موجود" });

            if (string.IsNullOrEmpty(template.TemplateFilePath))
                return BadRequest(new { message = "القالب لا يحتوي على ملف" });

            // بناء المسار الكامل للملف
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(webRootPath, template.TemplateFilePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { message = "ملف القالب غير موجود" });

            string content = "";

            try
            {
                // استخدام Xceed.Words.NET لقراءة المحتوى (نفس المكتبة اللي عندك)
                using (var doc = DocX.Load(fullPath))
                {
                    content = doc.Text;
                }

                _logger.LogInformation("Successfully read template content: {TemplateId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading template file: {TemplateId}", id);
                return StatusCode(500, new { message = "خطأ في قراءة ملف القالب" });
            }

            return Ok(new
            {
                id = template.Id,
                name = template.Name,
                content = content,
                filePath = template.TemplateFilePath
            });
        }

        // ================================================================
        // 🔄 تحويل جميع ملفات .doc إلى .docx (Admin Only)
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

            if (request.FilledData == null)
                request.FilledData = new Dictionary<string, string>();

            var template = await _contractService.GetTemplateByIdAsync(request.TemplateId);
            if (template == null)
                return BadRequest(new { message = "القالب غير موجود" });

            var result = await _contractService.GenerateContractFromTemplateAsync(
                userId.Value,
                request.TemplateId,
                request.FilledData,
                request.ContractTitle);

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

    // ================================================================
    // Request DTO
    // ================================================================

    public class GenerateContractRequest
    {
        public Guid TemplateId { get; set; }
        public Dictionary<string, string> FilledData { get; set; } = new();
        public string? ContractTitle { get; set; }
    }
}