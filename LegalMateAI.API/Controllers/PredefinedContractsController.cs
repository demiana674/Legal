// LegalMateAI.API/Controllers/PredefinedContractsController.cs
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
    [Authorize]
    [ApiController]
    [Route("api/contracts/predefined")]
    public class PredefinedContractsController : ControllerBase
    {
        private readonly IPredefinedContractService _service;
        private readonly ILogger<PredefinedContractsController> _logger;

        public PredefinedContractsController(
            IPredefinedContractService service,
            ILogger<PredefinedContractsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");
            
            if (claim == null)
                throw new UnauthorizedAccessException("User not authenticated");
            
            return Guid.Parse(claim.Value);
        }

        // ========== User Endpoints ==========

        /// <summary>
        /// جلب القوالب النشطة مع إمكانية البحث والفلترة
        /// </summary>
        [HttpGet("templates")]
        [ProducesResponseType(typeof(List<PredefinedContractTemplateDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveTemplates(
            [FromQuery] ContractType? type = null,
            [FromQuery] string? search = null,
            [FromQuery] bool featured = false)
        {
            var templates = await _service.GetActiveTemplatesAsync(type, search, featured);
            return Ok(templates);
        }

        /// <summary>
        /// البحث المتقدم في القوالب
        /// </summary>
        [HttpGet("templates/search")]
        [ProducesResponseType(typeof(List<PredefinedContractTemplateDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchTemplates(
            [FromQuery] string q,
            [FromQuery] ContractType? type = null)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "يرجى إدخال كلمة البحث" });

            var templates = await _service.SearchTemplatesAsync(q, type);
            return Ok(templates);
        }

        /// <summary>
        /// جلب القوالب المميزة
        /// </summary>
        [HttpGet("templates/featured")]
        [ProducesResponseType(typeof(List<PredefinedContractTemplateDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeaturedTemplates()
        {
            var templates = await _service.GetFeaturedTemplatesAsync();
            return Ok(templates);
        }

        /// <summary>
        /// جلب القوالب الأكثر استخداماً
        /// </summary>
        [HttpGet("templates/popular")]
        [ProducesResponseType(typeof(List<PredefinedContractTemplateDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPopularTemplates([FromQuery] int count = 5)
        {
            var templates = await _service.GetPopularTemplatesAsync(count);
            return Ok(templates);
        }

        /// <summary>
        /// جلب تفاصيل قالب واحد
        /// </summary>
        [HttpGet("templates/{id:guid}")]
        [ProducesResponseType(typeof(PredefinedContractTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplateById(Guid id)
        {
            var template = await _service.GetTemplateByIdAsync(id);
            if (template == null)
                return NotFound(new { message = "القالب غير موجود" });
            
            return Ok(template);
        }

        /// <summary>
        /// توليد عقد من قالب
        /// </summary>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(GeneratedContractDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateContract([FromBody] FillContractDataDto request)
        {
            var userId = GetUserId();
            
            var result = await _service.GenerateContractFromTemplateAsync(
                userId, 
                request.TemplateId, 
                request.FilledData,
                request.LawyerId,
                request.OutputFormat);
            
            if (result == null)
                return BadRequest(new { message = "فشل توليد العقد" });
            
            return Ok(result);
        }

        /// <summary>
        /// تحميل العقد المولد
        /// </summary>
        [HttpGet("download/{id:guid}")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadContract(Guid id)
        {
            var userId = GetUserId();
            var fileBytes = await _service.DownloadGeneratedContractAsync(userId, id);
            
            if (fileBytes == null)
                return NotFound(new { message = "العقد غير موجود" });
            
            return File(fileBytes, "application/pdf", $"contract_{id}.pdf");
        }

        /// <summary>
        /// جلب عقود المستخدم المولدة
        /// </summary>
        [HttpGet("my-contracts")]
        [ProducesResponseType(typeof(List<GeneratedContractDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyContracts()
        {
            var userId = GetUserId();
            var contracts = await _service.GetUserGeneratedContractsAsync(userId);
            return Ok(contracts);
        }

        /// <summary>
        /// حذف عقد مولّد
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteContract(Guid id)
        {
            var userId = GetUserId();
            var result = await _service.DeleteGeneratedContractAsync(userId, id);
            
            if (!result)
                return NotFound(new { message = "العقد غير موجود" });
            
            return Ok(new { message = "تم حذف العقد بنجاح" });
        }

        // ========== Admin Endpoints ==========

        /// <summary>
        /// رفع قالب جديد (Admin Only) - يدعم PDF و Word
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/templates/upload")]
        [ProducesResponseType(typeof(PredefinedContractTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadTemplate([FromForm] UploadPredefinedTemplateDto request)
        {
            var adminId = GetUserId();
            
            var result = await _service.UploadTemplateAsync(
                adminId,
                request.File,
                request.Name,
                request.NameEn,
                request.Description,
                request.ContractType,
                request.RequiredFields,
                request.SearchKeywords,
                request.IsFeatured);
            
            if (result == null)
                return BadRequest(new { message = "فشل رفع القالب" });
            
            return Ok(result);
        }

        /// <summary>
        /// جلب كل القوالب (Admin Only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/templates")]
        [ProducesResponseType(typeof(List<PredefinedContractTemplateDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTemplates(
            [FromQuery] bool includeInactive = true,
            [FromQuery] ContractType? type = null,
            [FromQuery] string? search = null)
        {
            var templates = await _service.GetAllTemplatesForAdminAsync(includeInactive, type, search);
            return Ok(templates);
        }

        /// <summary>
        /// تحديث قالب (Admin Only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("admin/templates/{id:guid}")]
        [ProducesResponseType(typeof(PredefinedContractTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdatePredefinedTemplateDto request)
        {
            var adminId = GetUserId();
            
            var result = await _service.UpdateTemplateAsync(
                adminId,
                id,
                request.Name,
                request.NameEn,
                request.Description,
                request.IsActive,
                request.IsFeatured,
                request.RequiredFields,
                request.SearchKeywords);
            
            if (result == null)
                return NotFound(new { message = "القالب غير موجود" });
            
            return Ok(result);
        }

        /// <summary>
        /// حذف قالب (Admin Only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/templates/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTemplate(Guid id)
        {
            var adminId = GetUserId();
            var result = await _service.DeleteTemplateAsync(adminId, id);
            
            if (!result)
                return NotFound(new { message = "القالب غير موجود" });
            
            return Ok(new { message = "تم حذف القالب بنجاح" });
        }
    }
}