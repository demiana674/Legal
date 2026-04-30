// LegalMateAI.API/Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        private Guid GetAdminId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id") ?? User.FindFirst("sub");
            if (claim == null) throw new UnauthorizedAccessException("Admin not authenticated");
            return Guid.Parse(claim.Value);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var dashboard = await _adminService.GetDashboardStatsAsync(GetAdminId());
            return Ok(dashboard);
        }

        [HttpGet("entity/{id}")]
        public async Task<IActionResult> GetEntityDetails(Guid id)
        {
            var entity = await _adminService.GetEntityDetailsAsync(id);
            return entity == null ? NotFound(new { message = "الكيان غير موجود" }) : Ok(entity);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterDto? filter)
        {
            return Ok(await _adminService.GetAllUsersAsync(filter ?? new UserFilterDto()));
        }

        [HttpPatch("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] AdminUpdateUserStatusDto request)
        {
            var result = await _adminService.UpdateUserStatusAsync(GetAdminId(), id, request.Status, request.Reason);
            return !result ? NotFound(new { message = "المستخدم غير موجود" }) : Ok(new { message = "تم تحديث حالة المستخدم بنجاح" });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var result = await _adminService.DeleteUserAsync(GetAdminId(), id);
            return !result ? NotFound(new { message = "المستخدم غير موجود" }) : Ok(new { message = "تم حذف المستخدم بنجاح" });
        }

        [HttpGet("lawyers")]
        public async Task<IActionResult> GetAllLawyers([FromQuery] LawyerFilterDto? filter)
        {
            return Ok(await _adminService.GetAllLawyersAsync(filter ?? new LawyerFilterDto()));
        }

        [HttpGet("lawyers/pending")]
        public async Task<IActionResult> GetPendingLawyers()
        {
            return Ok(await _adminService.GetPendingLawyersAsync());
        }

        [HttpPost("lawyers/{id}/approve")]
        public async Task<IActionResult> ApproveLawyer(Guid id)
        {
            var result = await _adminService.ApproveLawyerAsync(id);
            return !result ? NotFound(new { message = "المحامي غير موجود" }) : Ok(new { message = "تمت الموافقة على المحامي بنجاح" });
        }

        [HttpPost("lawyers/{id}/reject")]
        public async Task<IActionResult> RejectLawyer(Guid id, [FromBody] RejectRequest request)
        {
            var result = await _adminService.RejectLawyerAsync(id, request.Reason);
            return !result ? NotFound(new { message = "المحامي غير موجود" }) : Ok(new { message = "تم رفض المحامي" });
        }

        [HttpPost("lawyers/{id}/suspend")]
        public async Task<IActionResult> SuspendLawyer(Guid id, [FromBody] SuspendRequest request)
        {
            var result = await _adminService.SuspendLawyerAsync(id, request.Reason);
            return !result ? NotFound(new { message = "المحامي غير موجود" }) : Ok(new { message = "تم تعليق المحامي" });
        }

        [HttpPost("lawyers/{id}/activate")]
        public async Task<IActionResult> ActivateLawyer(Guid id)
        {
            var result = await _adminService.ActivateLawyerAsync(id);
            return !result ? NotFound(new { message = "المحامي غير موجود" }) : Ok(new { message = "تم تنشيط المحامي" });
        }

        [HttpDelete("lawyers/{id}")]
        public async Task<IActionResult> DeleteLawyer(Guid id)
        {
            var result = await _adminService.DeleteLawyerAsync(id);
            return !result ? NotFound(new { message = "المحامي غير موجود" }) : Ok(new { message = "تم حذف المحامي" });
        }

        [HttpGet("admins/{id}")]
        public async Task<IActionResult> GetAdminDetails(Guid id)
        {
            var admin = await _adminService.GetAdminDetailsAsync(id);
            return admin == null ? NotFound(new { message = "الأدمن غير موجود" }) : Ok(admin);
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] LogFilterDto? filter)
        {
            return Ok(await _adminService.GetLogsAsync(filter ?? new LogFilterDto()));
        }

        [HttpGet("logs/export")]
        public async Task<IActionResult> ExportLogs([FromQuery] LogFilterDto? filter, [FromQuery] string format = "csv")
        {
            var file = await _adminService.ExportLogsAsync(filter, format);
            var contentType = format.ToLower() == "pdf" ? "application/pdf" : "text/csv; charset=utf-8";
            return File(file, contentType, $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.{format}");
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            return Ok(await _adminService.GetSystemStatsAsync());
        }
    }

    public class RejectRequest { public string Reason { get; set; } = string.Empty; }
    public class SuspendRequest { public string? Reason { get; set; } }
}