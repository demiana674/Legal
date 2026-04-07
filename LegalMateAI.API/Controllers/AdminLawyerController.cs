// LegalMateAI.API/Controllers/AdminLawyerController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/lawyers")]
    public class AdminLawyerController : ControllerBase
    {
        private readonly IAdminLawyerService _adminLawyerService;
        private readonly ILogger<AdminLawyerController> _logger;

        public AdminLawyerController(
            IAdminLawyerService adminLawyerService,
            ILogger<AdminLawyerController> logger)
        {
            _adminLawyerService = adminLawyerService;
            _logger = logger;
        }

        private Guid GetAdminId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");
            
            if (claim == null)
            {
                throw new UnauthorizedAccessException("لم يتم العثور على معرف الأدمن");
            }
            
            return Guid.Parse(claim.Value);
        }

        [HttpGet("pending")]
        [ProducesResponseType(typeof(List<PendingLawyerDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingLawyers()
        {
            var lawyers = await _adminLawyerService.GetPendingLawyersAsync();
            return Ok(lawyers);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<LawyerResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllLawyers([FromQuery] LawyerFilterDto? filter)
        {
            filter ??= new LawyerFilterDto();
            var lawyers = await _adminLawyerService.GetAllLawyersAsync(filter);
            return Ok(lawyers);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LawyerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLawyerById(Guid id)
        {
            var lawyer = await _adminLawyerService.GetLawyerByIdAsync(id);
            
            if (lawyer == null)
                return NotFound(new { message = "المحامي غير موجود" });
            
            return Ok(lawyer);
        }

        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLawyerStatus(Guid id, [FromBody] UpdateLawyerStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            _logger.LogInformation($"Admin {adminId} updating lawyer {id} status to {request.Status}");

            var result = await _adminLawyerService.UpdateLawyerStatusAsync(id, request.Status, request.Notes);
            
            if (!result)
                return NotFound(new { message = "المحامي غير موجود" });
            
            return Ok(new { message = "تم تحديث حالة المحامي بنجاح" });
        }

        [HttpPost("{id}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveLawyer(Guid id)
        {
            var adminId = GetAdminId();
            _logger.LogInformation($"Admin {adminId} approving lawyer: {id}");
            
            var result = await _adminLawyerService.ApproveLawyerAsync(id);
            
            if (!result)
                return NotFound(new { message = "المحامي غير موجود" });
            
            return Ok(new { message = "تم الموافقة على المحامي بنجاح" });
        }

        [HttpPost("{id}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectLawyer(Guid id, [FromBody] RejectLawyerRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            _logger.LogInformation($"Admin {adminId} rejecting lawyer: {id}, Reason: {request.Reason}");

            var result = await _adminLawyerService.RejectLawyerAsync(id, request.Reason);
            
            if (!result)
                return NotFound(new { message = "المحامي غير موجود" });
            
            return Ok(new { message = "تم رفض طلب المحامي", reason = request.Reason });
        }

        [HttpPost("{id}/suspend")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SuspendLawyer(Guid id, [FromBody] SuspendLawyerRequest request)
        {
            var adminId = GetAdminId();
            _logger.LogInformation($"Admin {adminId} suspending lawyer: {id}, Reason: {request.Reason}");

            var result = await _adminLawyerService.SuspendLawyerAsync(id, request.Reason);
            
            if (!result)
                return NotFound(new { message = "المحامي غير موجود" });
            
            return Ok(new { message = "تم تعليق المحامي", reason = request.Reason });
        }

        [HttpPost("{id}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateLawyer(Guid id)
        {
            var adminId = GetAdminId();
            _logger.LogInformation($"Admin {adminId} activating lawyer: {id}");

            var result = await _adminLawyerService.ActivateLawyerAsync(id);
            
            if (!result)
                return NotFound(new { message = "المحامي غير موجود" });
            
            return Ok(new { message = "تم تنشيط المحامي بنجاح" });
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteLawyer(Guid id)
        {
            var adminId = GetAdminId();
            _logger.LogInformation($"Admin {adminId} deleting lawyer: {id}");

            var result = await _adminLawyerService.DeleteLawyerAsync(id);
            
            if (!result)
                return NotFound(new { message = "المحامي غير موجود" });
            
            return Ok(new { message = "تم حذف المحامي" });
        }
    }

    public class UpdateLawyerStatusRequest
    {
        [Required(ErrorMessage = "الحالة مطلوبة")]
        public LawyerVerificationStatus Status { get; set; }
        
        public string? Notes { get; set; }
    }

    public class RejectLawyerRequest
    {
        [Required(ErrorMessage = "سبب الرفض مطلوب")]
        public string Reason { get; set; } = string.Empty;
    }

    public class SuspendLawyerRequest
    {
        public string? Reason { get; set; }
    }
}