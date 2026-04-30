// LegalMateAI.API/Controllers/ContractsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _contractService;
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(IContractService contractService, ILogger<ContractsController> logger)
        {
            _contractService = contractService;
            _logger = logger;
        }

        private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private bool IsLawyer() => User.IsInRole("Lawyer");

        [AllowAnonymous]
        [HttpGet("templates")]
        public async Task<IActionResult> GetAllTemplates([FromQuery] ContractType? type = null, [FromQuery] string? search = null)
        {
            return Ok(await _contractService.GetContractTemplatesAsync(type, search));
        }

        [AllowAnonymous]
        [HttpGet("templates/{id}")]
        public async Task<IActionResult> GetTemplateById(Guid id)
        {
            var template = await _contractService.GetTemplateByIdAsync(id);
            return template == null ? NotFound(new { message = "القالب غير موجود" }) : Ok(template);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateContract([FromBody] GenerateContractRequest request)
        {
            var template = await _contractService.GetTemplateByIdAsync(request.TemplateId);
            if (template == null) return BadRequest(new { message = "القالب غير موجود. يجب اختيار قالب صحيح." });

            var result = await _contractService.GenerateContractFromTemplateAsync(GetUserId(), request);
            if (result == null) return BadRequest(new { message = "فشل توليد العقد" });

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyContracts([FromQuery] string? search = null, [FromQuery] ContractStatus? status = null)
        {
            var userId = GetUserId();
            return Ok(IsLawyer() ? await _contractService.GetLawyerContractsAsync(userId, status?.ToString(), search) : await _contractService.GetUserContractsAsync(userId, status?.ToString(), search));
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchContracts([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { message = "يرجى إدخال كلمة البحث" });
            return Ok(await _contractService.SearchContractsAsync(GetUserId(), q, IsLawyer()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetContractById(Guid id)
        {
            var contract = await _contractService.GetContractByIdAsync(GetUserId(), id, IsLawyer());
            return contract == null ? NotFound(new { message = "العقد غير موجود" }) : Ok(contract);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContract(Guid id, [FromBody] UpdateContractDto request)
        {
            var result = await _contractService.UpdateContractAsync(GetUserId(), id, request);
            return result == null ? NotFound(new { message = "العقد غير موجود" }) : Ok(result);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateContractStatus(Guid id, [FromBody] UpdateContractStatusDto request)
        {
            var result = await _contractService.UpdateContractStatusAsync(GetUserId(), id, request, IsLawyer());
            return !result ? NotFound(new { message = "العقد غير موجود" }) : Ok(new { message = "تم تحديث حالة العقد بنجاح" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContract(Guid id)
        {
            var result = await _contractService.DeleteContractAsync(GetUserId(), id);
            return !result ? NotFound(new { message = "العقد غير موجود" }) : Ok(new { message = "تم حذف العقد بنجاح" });
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadContract(Guid id)
        {
            var fileBytes = await _contractService.DownloadContractAsync(GetUserId(), id);
            if (fileBytes == null) return NotFound(new { message = "العقد غير موجود" });

            var contract = await _contractService.GetContractByIdAsync(GetUserId(), id, IsLawyer());
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{contract?.Title ?? "contract"}.docx");
        }
    }
}