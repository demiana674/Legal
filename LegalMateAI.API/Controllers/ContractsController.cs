// LegalMateAI.API/Controllers/ContractsController.cs
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

        // ================================================================
        // 📋 قوالب العقود (Public)
        // ================================================================

        [AllowAnonymous]
        [HttpGet("templates")]
        public async Task<IActionResult> GetContractTemplates(
            [FromQuery] ContractType? type = null, 
            [FromQuery] string? search = null)
        {
            return Ok(await _contractService.GetContractTemplatesAsync(type, search));
        }

        [AllowAnonymous]
        [HttpGet("templates/{id:guid}")]
        public async Task<IActionResult> GetContractTemplateById(Guid id)
        {
            var template = await _contractService.GetTemplateByIdAsync(id);
            return template == null ? NotFound(new { message = "القالب غير موجود" }) : Ok(template);
        }

        // ================================================================
        // 📄 توليد العقود (Auth Required)
        // ================================================================

        [Authorize]
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateContract([FromBody] GenerateContractRequest request)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var template = await _contractService.GetTemplateByIdAsync(request.TemplateId);
            if (template == null) return BadRequest(new { message = "القالب غير موجود" });

            var result = await _contractService.GenerateContractFromTemplateAsync(userId.Value, request);
            return result == null ? BadRequest(new { message = "فشل توليد العقد" }) : Ok(result);
        }

        // ================================================================
        // 📁 عقودي (Auth Required)
        // ================================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyContracts(
            [FromQuery] string? search = null, 
            [FromQuery] ContractStatus? status = null)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var contracts = await _contractService.GetUserContractsAsync(userId.Value, status?.ToString(), search);
            return Ok(contracts);
        }

        // ================================================================
        // 🔍 بحث عام (Public)
        // ================================================================

        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<IActionResult> SearchContracts([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { message = "يرجى إدخال كلمة البحث" });

            var contracts = await _contractService.SearchAllContractsAsync(q);
            return Ok(contracts);
        }

        // ================================================================
        // 👀 عرض أي عقد (Public)
        // ================================================================

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetContractById(Guid id)
        {
            var contract = await _contractService.GetAnyContractByIdAsync(id);
            return contract == null ? NotFound(new { message = "العقد غير موجود" }) : Ok(contract);
        }

        // ================================================================
        // ✏️ تعديل عقدي فقط (Auth Required - مالك العقد فقط)
        // ================================================================

        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateContract(Guid id, [FromBody] UpdateContractDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _contractService.UpdateContractAsync(userId.Value, id, request);
            return result == null ? NotFound(new { message = "العقد غير موجود أو غير مصرح لك بتعديله" }) : Ok(result);
        }

        [Authorize]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateContractStatus(Guid id, [FromBody] UpdateContractStatusDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _contractService.UpdateContractStatusAsync(userId.Value, id, request);
            return !result ? NotFound(new { message = "العقد غير موجود أو غير مصرح لك" }) : Ok(new { message = "تم تحديث حالة العقد بنجاح" });
        }

        // ================================================================
        // 🗑️ حذف عقدي فقط (Auth Required - مالك العقد فقط)
        // ================================================================

        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteContract(Guid id)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _contractService.DeleteContractAsync(userId.Value, id);
            return !result ? NotFound(new { message = "العقد غير موجود أو غير مصرح لك بحذفه" }) : Ok(new { message = "تم حذف العقد بنجاح" });
        }

        // ================================================================
        // 📥 تحميل أي عقد (Public)
        // ================================================================

        [AllowAnonymous]
        [HttpGet("{id:guid}/download")]
        public async Task<IActionResult> DownloadContract(Guid id)
        {
            var fileBytes = await _contractService.DownloadAnyContractAsync(id);
            if (fileBytes == null) return NotFound(new { message = "العقد غير موجود" });

            var contract = await _contractService.GetAnyContractByIdAsync(id);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{contract?.Title ?? "contract"}.docx");
        }
    }
}