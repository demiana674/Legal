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
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }

        private bool IsLawyer()
        {
            return GetUserRole() == "Lawyer";
        }

        /// <summary>
        /// حجز موعد جديد
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AppointmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            var result = await _appointmentService.CreateAppointmentAsync(userId, request);
            
            if (result == null)
            {
                return BadRequest(ApiResponse<object>.BadRequest("الموعد غير متاح أو المحامي غير موجود"));
            }

            _logger.LogInformation("Appointment created: {AppointmentId}", result.Id);
            return Ok(ApiResponse<AppointmentResponseDto>.Ok(result, "تم حجز الموعد بنجاح"));
        }

        /// <summary>
        /// الحصول على مواعيد المستخدم
        /// </summary>
        [HttpGet("user")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserAppointments([FromQuery] string? status)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            var appointments = await _appointmentService.GetUserAppointmentsAsync(userId, status);
            return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok(appointments));
        }

        /// <summary>
        /// الحصول على مواعيد المحامي
        /// </summary>
        [HttpGet("lawyer")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentResponseDto>>), StatusCodes.Status200OK)]
        [Authorize(Roles = "Lawyer")]
        public async Task<IActionResult> GetLawyerAppointments([FromQuery] string? status)
        {
            var lawyerId = GetUserId();
            if (lawyerId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            var appointments = await _appointmentService.GetLawyerAppointmentsAsync(lawyerId, status);
            return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok(appointments));
        }

        /// <summary>
        /// الحصول على موعد محدد مع التحقق من الصلاحية
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAppointmentById(Guid id)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            
            if (appointment == null)
            {
                return NotFound(ApiResponse<object>.NotFound("الموعد غير موجود"));
            }

            // Resource-based Authorization
            bool isOwner = appointment.UserId == userId || appointment.LawyerId == userId;
            if (!isOwner && userRole != "Admin")
            {
                _logger.LogWarning("Unauthorized access: User {UserId} tried to view appointment {AppointmentId}", userId, id);
                // ✅ Forbid() لا يقبل معاملات، نستخدم StatusCode(403)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Unauthorized("غير مصرح لك بمشاهدة هذا الموعد"));
            }

            return Ok(ApiResponse<AppointmentResponseDto>.Ok(appointment));
        }

        /// <summary>
        /// إلغاء موعد مع التحقق من الصلاحية
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CancelAppointment(Guid id, [FromBody] string? reason)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                return NotFound(ApiResponse<object>.NotFound("الموعد غير موجود"));
            }

            // Resource-based Authorization
            bool isParticipant = appointment.UserId == userId || appointment.LawyerId == userId;
            if (!isParticipant && userRole != "Admin")
            {
                _logger.LogWarning("Unauthorized cancel: User {UserId} tried to cancel appointment {AppointmentId}", userId, id);
                // ✅ Forbid() لا يقبل معاملات، نستخدم StatusCode(403)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Unauthorized("غير مصرح لك بإلغاء هذا الموعد"));
            }

            var result = await _appointmentService.CancelAppointmentAsync(id, reason);
            
            if (!result)
            {
                return NotFound(ApiResponse<object>.NotFound("الموعد غير موجود أو لا يمكن إلغاؤه"));
            }

            _logger.LogInformation("Appointment cancelled: {AppointmentId}", id);
            return Ok(ApiResponse<object>.Ok("تم إلغاء الموعد بنجاح"));
        }

        /// <summary>
        /// طلب إعادة جدولة موعد مع التحقق من الصلاحية
        /// </summary>
        [HttpPost("{id}/reschedule")]
        [ProducesResponseType(typeof(ApiResponse<RescheduleResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RequestReschedule(Guid id, [FromBody] CreateRescheduleRequestDto request)
        {
            var userId = GetUserId();
            
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                return NotFound(ApiResponse<object>.NotFound("الموعد غير موجود"));
            }

            // Resource-based Authorization
            bool isParticipant = appointment.UserId == userId || appointment.LawyerId == userId;
            if (!isParticipant)
            {
                _logger.LogWarning("Unauthorized reschedule: User {UserId} tried to reschedule appointment {AppointmentId}", userId, id);
                // ✅ Forbid() لا يقبل معاملات، نستخدم StatusCode(403)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Unauthorized("غير مصرح لك بإعادة جدولة هذا الموعد"));
            }

            request.AppointmentId = id;
            var result = await _appointmentService.RequestRescheduleAsync(userId, request, IsLawyer());
            
            if (result == null)
            {
                return BadRequest(ApiResponse<object>.BadRequest("لا يمكن إعادة جدولة الموعد"));
            }

            _logger.LogInformation("Reschedule requested: Appointment {AppointmentId}", id);
            return Ok(ApiResponse<RescheduleResponseDto>.Ok(result, "تم طلب إعادة الجدولة بنجاح"));
        }

        /// <summary>
        /// الموافقة أو رفض طلب إعادة الجدولة
        /// </summary>
        [HttpPut("reschedule/{rescheduleId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RespondToReschedule(Guid rescheduleId, [FromBody] UpdateRescheduleRequestDto request)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            var result = await _appointmentService.RespondToRescheduleAsync(userId, rescheduleId, request, IsLawyer());
            
            if (!result)
            {
                return NotFound(ApiResponse<object>.NotFound("طلب إعادة الجدولة غير موجود"));
            }

            var message = request.Status == RescheduleStatus.Approved 
                ? "تم قبول طلب إعادة الجدولة" 
                : "تم رفض طلب إعادة الجدولة";
                
            return Ok(ApiResponse<object>.Ok(message));
        }

        /// <summary>
        /// جلب طلبات إعادة الجدولة للمستخدم الحالي
        /// </summary>
        [HttpGet("reschedule/pending")]
        [ProducesResponseType(typeof(ApiResponse<List<RescheduleResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingRescheduleRequests()
        {
            var userId = GetUserId();
            
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<object>.Unauthorized());
            }

            List<RescheduleResponseDto> requests;
            
            if (IsLawyer())
            {
                requests = await _appointmentService.GetRescheduleRequestsForLawyerAsync(userId);
            }
            else
            {
                requests = await _appointmentService.GetRescheduleRequestsForUserAsync(userId);
            }
            
            return Ok(ApiResponse<List<RescheduleResponseDto>>.Ok(requests));
        }
    }
}