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
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");
            
            if (claim == null)
                throw new UnauthorizedAccessException("Admin not authenticated");
            
            return Guid.Parse(claim.Value);
        }

        // ========== Dashboard ==========
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var adminId = GetAdminId();
            var dashboard = await _adminService.GetDashboardStatsAsync(adminId);
            return Ok(dashboard);
        }

        // ========== User Management ==========
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterDto? filter)
        {
            var users = await _adminService.GetAllUsersAsync(filter ?? new UserFilterDto());
            return Ok(users);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserDetails(Guid id)
        {
            var user = await _adminService.GetUserDetailsAsync(id);
            return user == null ? NotFound(new { message = "المستخدم غير موجود" }) : Ok(user);
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

        // ========== Lawyer Management ==========
        [HttpGet("lawyers")]
        public async Task<IActionResult> GetAllLawyers([FromQuery] LawyerFilterDto? filter)
        {
            var lawyers = await _adminService.GetAllLawyersAsync(filter ?? new LawyerFilterDto());
            return Ok(lawyers);
        }

        [HttpGet("lawyers/pending")]
        public async Task<IActionResult> GetPendingLawyers()
        {
            var lawyers = await _adminService.GetPendingLawyersAsync();
            return Ok(lawyers);
        }

        [HttpGet("lawyers/{id}")]
        public async Task<IActionResult> GetLawyerById(Guid id)
        {
            var lawyer = await _adminService.GetLawyerDetailsAsync(id);
            return lawyer == null ? NotFound(new { message = "المحامي غير موجود" }) : Ok(lawyer);
        }

        [HttpPost("lawyers/{id}/approve")]
        public async Task<IActionResult> ApproveLawyer(Guid id)
        {
            var result = await _adminService.ApproveLawyerAsync(id);
            return !result ? NotFound(new { message = "المحامي غير موجود" }) : Ok(new { message = "تمت الموافقة على المحامي بنجاح" });
        }

        [HttpPost("lawyers/{id}/reject")]
        public async Task<IActionResult> RejectLawyer(Guid id, [FromBody] RejectLawyerRequest request)
        {
            var result = await _adminService.RejectLawyerAsync(id, request.Reason);
            return !result ? NotFound(new { message = "المحامي غير موجود" }) : Ok(new { message = "تم رفض المحامي" });
        }

        [HttpPost("lawyers/{id}/suspend")]
        public async Task<IActionResult> SuspendLawyer(Guid id, [FromBody] SuspendLawyerRequest request)
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

        // ========== Log Management ==========
        [HttpGet("logs")]
        public async Task<IActionResult> GetAdminLogs([FromQuery] LogFilterDto? filter)
        {
            var logs = await _adminService.GetAdminLogsAsync(filter ?? new LogFilterDto());
            return Ok(logs);
        }

        [HttpGet("logs/export")]
        public async Task<IActionResult> ExportLogs([FromQuery] LogFilterDto? filter, [FromQuery] string format = "csv")
        {
            var file = await _adminService.ExportLogsAsync(filter, format);
            var contentType = format.ToLower() == "pdf" ? "application/pdf" : "text/csv; charset=utf-8";
            return File(file, contentType, $"admin_logs_{DateTime.Now:yyyyMMdd_HHmmss}.{format}");
        }

        // ========== System Stats ==========
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            var stats = await _adminService.GetSystemStatsAsync();
            return Ok(stats);
        }

        [HttpPost("clear-cache")]
        public async Task<IActionResult> ClearCache()
        {
            await _adminService.ClearCacheAsync(GetAdminId());
            return Ok(new { message = "تم مسح الذاكرة المؤقتة بنجاح" });
        }
    }

    public class RejectLawyerRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class SuspendLawyerRequest
    {
        public string? Reason { get; set; }
    }
}