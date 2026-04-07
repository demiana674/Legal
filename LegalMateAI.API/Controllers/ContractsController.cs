// LegalMateAI.API/Controllers/ContractsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using System.Security.Claims;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractsController(IContractService contractService)
        {
            _contractService = contractService;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!.Value);
        }

        private bool IsLawyer()
        {
            return User.IsInRole("Lawyer");
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        // ========== Contract CRUD ==========

        // POST: api/contracts
        [HttpPost]
        [ProducesResponseType(typeof(ContractResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateContract([FromBody] CreateContractDto request)
        {
            var userId = GetUserId();
            var result = await _contractService.CreateContractAsync(userId, request);

            if (result == null)
                return BadRequest(new { message = "فشل إنشاء العقد" });

            return Ok(result);
        }

        // POST: api/contracts/from-template/{templateId}
        [HttpPost("from-template/{templateId}")]
        [ProducesResponseType(typeof(ContractResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateContractFromTemplate(Guid templateId, [FromBody] Dictionary<string, string> customFields)
        {
            var userId = GetUserId();
            var result = await _contractService.CreateContractFromTemplateAsync(userId, templateId, customFields);

            if (result == null)
                return BadRequest(new { message = "فشل إنشاء العقد من القالب" });

            return Ok(result);
        }

        // GET: api/contracts
        [HttpGet]
        [ProducesResponseType(typeof(List<ContractResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyContracts([FromQuery] string? status)
        {
            var userId = GetUserId();

            if (IsLawyer())
            {
                var contracts = await _contractService.GetLawyerContractsAsync(userId, status);
                return Ok(contracts);
            }
            else
            {
                var contracts = await _contractService.GetUserContractsAsync(userId, status);
                return Ok(contracts);
            }
        }

        // GET: api/contracts/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ContractResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetContractById(Guid id)
        {
            var userId = GetUserId();
            var isLawyer = IsLawyer();
            var contract = await _contractService.GetContractByIdAsync(userId, id, isLawyer);

            if (contract == null)
                return NotFound(new { message = "العقد غير موجود" });

            return Ok(contract);
        }

        // PUT: api/contracts/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ContractResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateContract(Guid id, [FromBody] UpdateContractDto request)
        {
            var userId = GetUserId();
            var result = await _contractService.UpdateContractAsync(userId, id, request);

            if (result == null)
                return NotFound(new { message = "العقد غير موجود أو لا يمكن تعديله" });

            return Ok(result);
        }

        // PATCH: api/contracts/{id}/status
        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateContractStatus(Guid id, [FromBody] UpdateContractStatusDto request)
        {
            var userId = GetUserId();
            var isLawyer = IsLawyer();
            var result = await _contractService.UpdateContractStatusAsync(userId, id, request, isLawyer);

            if (!result)
                return NotFound(new { message = "العقد غير موجود أو لا يمكن تحديث حالته" });

            return Ok(new { message = "تم تحديث حالة العقد بنجاح" });
        }

        // DELETE: api/contracts/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteContract(Guid id)
        {
            var userId = GetUserId();
            var result = await _contractService.DeleteContractAsync(userId, id);

            if (!result)
                return NotFound(new { message = "العقد غير موجود أو لا يمكن حذفه" });

            return Ok(new { message = "تم حذف العقد بنجاح" });
        }

        // GET: api/contracts/{id}/download
        [HttpGet("{id}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
       


public async Task<IActionResult> DownloadContract(Guid id, string format = "pdf")
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim))
        return Unauthorized("UserId claim not found in token");

    var userId = Guid.Parse(userIdClaim);

    var fileBytes = await _contractService.DownloadContractAsync(userId, id, format);

    if (fileBytes == null)
        return NotFound();

    if (format.ToLower() == "pdf")
        return File(fileBytes, "application/pdf", $"contract_{id}.pdf");

    if (format.ToLower() == "doc" || format.ToLower() == "docx")
        return File(fileBytes, "application/msword", $"contract_{id}.doc");

    return File(fileBytes, "text/plain", $"contract_{id}.txt");
}

        // ========== Contract Templates (Public) ==========

        // GET: api/contracts/templates
        [HttpGet("templates")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<ContractTemplateResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllContractTemplates([FromQuery] ContractType? type)
        {
            var templates = await _contractService.GetContractTemplatesAsync(type);
            return Ok(templates);
        }

        // GET: api/contracts/templates/{type}
        [HttpGet("templates/{type}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ContractTemplateResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetContractTemplateByType(string type)
        {
            var templates = await _contractService.GetContractTemplatesAsync();
            var template = templates.FirstOrDefault(t => t.Type.ToString().ToLower() == type.ToLower());
            
            if (template == null)
                return NotFound(new { message = "القالب غير موجود" });
            
            return Ok(template);
        }

        // ========== Contract Templates (Admin Only) ==========

        // POST: api/contracts/templates
        [Authorize(Roles = "Admin")]
        [HttpPost("templates")]
        [ProducesResponseType(typeof(ContractTemplateResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateContractTemplate([FromBody] CreateContractTemplateDto request)
        {
            var adminId = GetUserId();
            var result = await _contractService.CreateContractTemplateAsync(adminId, request);

            if (result == null)
                return BadRequest(new { message = "فشل إنشاء القالب" });

            return Ok(result);
        }

        // PUT: api/contracts/templates/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("templates/{id}")]
        [ProducesResponseType(typeof(ContractTemplateResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateContractTemplate(Guid id, [FromBody] UpdateContractTemplateDto request)
        {
            var adminId = GetUserId();
            var result = await _contractService.UpdateContractTemplateAsync(adminId, id, request);

            if (result == null)
                return NotFound(new { message = "القالب غير موجود" });

            return Ok(result);
        }

        // DELETE: api/contracts/templates/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("templates/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteContractTemplate(Guid id)
        {
            var adminId = GetUserId();
            var result = await _contractService.DeleteContractTemplateAsync(adminId, id);

            if (!result)
                return NotFound(new { message = "القالب غير موجود" });

            return Ok(new { message = "تم حذف القالب بنجاح" });
        }
    }
}