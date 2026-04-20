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

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!.Value);
        }

        private bool IsLawyer() => User.IsInRole("Lawyer");

        // ========== القوالب (للقراءة فقط) ==========

        /// <summary>
        /// الحصول على جميع القوالب المتاحة
        /// </summary>
        [AllowAnonymous]
        [HttpGet("templates")]
        [ProducesResponseType(typeof(List<ContractTemplateResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTemplates([FromQuery] ContractType? type = null, [FromQuery] string? search = null)
        {
            var templates = await _contractService.GetContractTemplatesAsync(type, search);
            return Ok(templates);
        }

        /// <summary>
        /// الحصول على قالب محدد
        /// </summary>
        [AllowAnonymous]
        [HttpGet("templates/{id}")]
        [ProducesResponseType(typeof(ContractTemplateResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplateById(Guid id)
        {
            var template = await _contractService.GetTemplateByIdAsync(id);
            if (template == null)
                return NotFound(new { message = "القالب غير موجود" });
            return Ok(template);
        }

        /// <summary>
        /// تحميل ملف القالب
        /// </summary>
        [AllowAnonymous]
        [HttpGet("templates/{id}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadTemplate(Guid id)
        {
            var template = await _contractService.GetTemplateByIdAsync(id);
            if (template == null)
                return NotFound(new { message = "القالب غير موجود" });

            var fileBytes = await _contractService.DownloadTemplateAsync(id);
            if (fileBytes == null)
                return NotFound(new { message = "الملف غير موجود" });

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{template.Name}.docx");
        }

        // ========== عقود المستخدم ==========

        /// <summary>
        /// توليد عقد من قالب
        /// </summary>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(ContractResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateContract([FromBody] GenerateContractRequest request)
        {
            var userId = GetUserId();
            
            var result = await _contractService.GenerateContractFromTemplateAsync(userId, request);
            if (result == null)
                return BadRequest(new { message = "فشل توليد العقد" });

            _logger.LogInformation($"Contract generated: {result.Id}");
            return Ok(result);
        }

        /// <summary>
        /// الحصول على عقود المستخدم
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ContractResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyContracts([FromQuery] string? search = null, [FromQuery] ContractStatus? status = null)
        {
            var userId = GetUserId();

            if (IsLawyer())
            {
                var contracts = await _contractService.GetLawyerContractsAsync(userId, status?.ToString(), search);
                return Ok(contracts);
            }
            else
            {
                var contracts = await _contractService.GetUserContractsAsync(userId, status?.ToString(), search);
                return Ok(contracts);
            }
        }

        /// <summary>
        /// البحث في العقود
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<ContractResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchContracts([FromQuery] string q)
        {
            var userId = GetUserId();
            
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "يرجى إدخال كلمة البحث" });

            var contracts = await _contractService.SearchContractsAsync(userId, q, IsLawyer());
            return Ok(contracts);
        }

        /// <summary>
        /// الحصول على عقد محدد
        /// </summary>
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

        /// <summary>
        /// تحديث بيانات العقد
        /// </summary>
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

        /// <summary>
        /// تحديث حالة العقد
        /// </summary>
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

        /// <summary>
        /// حذف عقد
        /// </summary>
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

        /// <summary>
        /// تحميل العقد
        /// </summary>
        [HttpGet("{id}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadContract(Guid id)
        {
            var userId = GetUserId();
            var fileBytes = await _contractService.DownloadContractAsync(userId, id);

            if (fileBytes == null)
                return NotFound(new { message = "العقد غير موجود" });

            var contract = await _contractService.GetContractByIdAsync(userId, id, IsLawyer());
            var fileName = contract?.Title ?? "contract";
            
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{fileName}.docx");
        }
    }

    // ✅ تم حذف تعريف GenerateContractRequest من هنا
    // لأنه موجود في IContractService
}