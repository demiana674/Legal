using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(
            IAppointmentService appointmentService,
            ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }

        private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        private bool IsLawyer() => GetUserRole() == "Lawyer";

        // ================================================================
        // 📅 حجز موعد جديد
        // ================================================================

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AppointmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<object>.Unauthorized());

            var result = await _appointmentService.CreateAppointmentAsync(userId, request);
            if (result == null)
                return BadRequest(ApiResponse<object>.BadRequest("الموعد غير متاح أو المحامي غير موجود"));

            _logger.LogInformation("Appointment created: {AppointmentId}", result.Id);
            return Ok(ApiResponse<AppointmentResponseDto>.Ok(result, "تم حجز الموعد بنجاح - في انتظار موافقة المحامي"));
        }

        // ================================================================
        // 📋 مواعيدي (User)
        // ================================================================

        [HttpGet("user")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserAppointments([FromQuery] string? status = null)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<object>.Unauthorized());

            var appointments = await _appointmentService.GetUserAppointmentsAsync(userId, status);
            return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok(appointments));
        }

        // ================================================================
        // 📋 مواعيدي (Lawyer)
        // ================================================================

        [HttpGet("lawyer")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentResponseDto>>), StatusCodes.Status200OK)]
        [Authorize(Roles = "Lawyer")]
        public async Task<IActionResult> GetLawyerAppointments([FromQuery] string? status = null)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<object>.Unauthorized());

            var appointments = await _appointmentService.GetLawyerAppointmentsAsync(userId, status);
            return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok(appointments));
        }

        // ================================================================
        // 📋 طلبات معلقة للمحامي (Pending Approval)
        // ================================================================

        [HttpGet("lawyer/pending")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentResponseDto>>), StatusCodes.Status200OK)]
        [Authorize(Roles = "Lawyer")]
        public async Task<IActionResult> GetPendingAppointments()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<object>.Unauthorized());

            var appointments = await _appointmentService.GetLawyerAppointmentsAsync(userId, "Pending");
            return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok(appointments));
        }

        // ================================================================
        // ✅ المحامي يقبل موعد
        // ================================================================

        [HttpPost("{id}/approve")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Roles = "Lawyer")]
        public async Task<IActionResult> ApproveAppointment(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<object>.Unauthorized());

            var result = await _appointmentService.ApproveAppointmentAsync(userId, id);
            if (!result)
                return NotFound(ApiResponse<object>.NotFound("الموعد غير موجود أو غير مصرح لك"));

            return Ok(ApiResponse<object>.Ok("تم تأكيد الموعد بنجاح"));
        }

        // ================================================================
        // ❌ المحامي يرفض موعد
        // ================================================================

        [HttpPost("{id}/reject")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Roles = "Lawyer")]
        public async Task<IActionResult> RejectAppointment(Guid id, [FromBody] string? reason)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<object>.Unauthorized());

            var result = await _appointmentService.RejectAppointmentAsync(userId, id, reason);
            if (!result)
                return NotFound(ApiResponse<object>.NotFound("الموعد غير موجود أو غير مصرح لك"));

            return Ok(ApiResponse<object>.Ok("تم رفض الموعد"));
        }

        // ================================================================
        // 👀 تفاصيل موعد
        // ================================================================

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppointmentById(Guid id)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            if (userId == Guid.Empty) return Unauthorized(ApiResponse<object>.Unauthorized());

            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null) return NotFound(ApiResponse<object>.NotFound("الموعد غير موجود"));

            bool isOwner = appointment.UserId == userId || appointment.LawyerId == userId;
            if (!isOwner && userRole != "Admin")
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Unauthorized("غير مصرح لك بمشاهدة هذا الموعد"));

            return Ok(ApiResponse<AppointmentResponseDto>.Ok(appointment));
        }

        // ================================================================
        // 🗑️ إلغاء موعد (بموافقة الطرفين)
        // ================================================================

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelAppointment(Guid id, [FromBody] string? reason = null)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            if (userId == Guid.Empty) return Unauthorized(ApiResponse<object>.Unauthorized());

            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null) return NotFound(ApiResponse<object>.NotFound("الموعد غير موجود"));

            bool isParticipant = appointment.UserId == userId || appointment.LawyerId == userId;
            if (!isParticipant && userRole != "Admin")
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Unauthorized("غير مصرح لك بإلغاء هذا الموعد"));

            var result = await _appointmentService.CancelAppointmentAsync(id, userId, reason);
            if (!result) 
                return BadRequest(ApiResponse<object>.BadRequest("لا يمكن إلغاء هذا الموعد"));

            return Ok(ApiResponse<object>.Ok("تم إلغاء الموعد بنجاح"));
        }

        // ================================================================
        // 🔄 طلب إعادة جدولة
        // ================================================================

        [HttpPost("{id}/reschedule")]
        [ProducesResponseType(typeof(ApiResponse<RescheduleResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestReschedule(Guid id, [FromBody] CreateRescheduleRequestDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized(ApiResponse<object>.Unauthorized());

            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null) return NotFound(ApiResponse<object>.NotFound("الموعد غير موجود"));

            bool isParticipant = appointment.UserId == userId || appointment.LawyerId == userId;
            if (!isParticipant)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Unauthorized("غير مصرح لك بإعادة جدولة هذا الموعد"));

            request.AppointmentId = id;
            var result = await _appointmentService.RequestRescheduleAsync(userId, request, IsLawyer());
            if (result == null) return BadRequest(ApiResponse<object>.BadRequest("لا يمكن إعادة جدولة الموعد - الوقت غير متاح"));

            return Ok(ApiResponse<RescheduleResponseDto>.Ok(result, "تم طلب إعادة الجدولة بنجاح"));
        }

        // ================================================================
        // ✅ الموافقة/رفض إعادة الجدولة
        // ================================================================

        [HttpPut("reschedule/{rescheduleId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RespondToReschedule(Guid rescheduleId, [FromBody] UpdateRescheduleRequestDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized(ApiResponse<object>.Unauthorized());

            var result = await _appointmentService.RespondToRescheduleAsync(userId, rescheduleId, request, IsLawyer());
            if (!result) return NotFound(ApiResponse<object>.NotFound("طلب إعادة الجدولة غير موجود"));

            var message = request.Status == RescheduleStatus.Approved ? "تم قبول طلب إعادة الجدولة" : "تم رفض طلب إعادة الجدولة";
            return Ok(ApiResponse<object>.Ok(message));
        }

        // ================================================================
        // 📋 طلبات إعادة الجدولة المعلقة
        // ================================================================

        [HttpGet("reschedule/pending")]
        [ProducesResponseType(typeof(ApiResponse<List<RescheduleResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingRescheduleRequests()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized(ApiResponse<object>.Unauthorized());

            List<RescheduleResponseDto> requests;
            if (IsLawyer())
                requests = await _appointmentService.GetRescheduleRequestsForLawyerAsync(userId);
            else
                requests = await _appointmentService.GetRescheduleRequestsForUserAsync(userId);

            return Ok(ApiResponse<List<RescheduleResponseDto>>.Ok(requests));
        }
    }
}