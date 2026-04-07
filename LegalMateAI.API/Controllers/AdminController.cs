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
            if (claim == null)
            {
                claim = User.FindFirst("id");
            }
            if (claim == null)
            {
                claim = User.FindFirst("sub");
            }
            
            if (claim == null)
            {
                _logger.LogWarning("No admin ID claim found");
                throw new UnauthorizedAccessException("Admin not authenticated");
            }
            
            return Guid.Parse(claim.Value);
        }

        // ========== Dashboard ==========
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard()
        {
            var adminId = GetAdminId();
            var dashboard = await _adminService.GetDashboardStatsAsync(adminId);
            return Ok(dashboard);
        }

        // ========== User Management ==========
        
        // GET: api/admin/users
        [HttpGet("users")]
        [ProducesResponseType(typeof(List<UserResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterDto? filter)
        {
            filter ??= new UserFilterDto();
            var users = await _adminService.GetAllUsersAsync(filter);
            return Ok(users);
        }

        // GET: api/admin/users/{id}
        [HttpGet("users/{id}")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserDetails(Guid id)
        {
            var user = await _adminService.GetUserDetailsAsync(id);

            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            return Ok(user);
        }

        // PATCH: api/admin/users/{id}/status
        [HttpPatch("users/{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] AdminUpdateUserStatusDto request)
        {
            var adminId = GetAdminId();
            _logger.LogInformation($"Admin {adminId} updating status of user {id} to {request.Status}");
            
            var result = await _adminService.UpdateUserStatusAsync(adminId, id, request.Status, request.Reason);

            if (!result)
                return NotFound(new { message = "المستخدم غير موجود" });

            _logger.LogInformation($"User {id} status updated to {request.Status}");
            return Ok(new { message = "تم تحديث حالة المستخدم بنجاح" });
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var adminId = GetAdminId();
            var result = await _adminService.DeleteUserAsync(adminId, id);

            if (!result)
                return NotFound(new { message = "المستخدم غير موجود" });

            return Ok(new { message = "تم حذف المستخدم بنجاح" });
        }

        // ========== Log Management ==========
        
        // GET: api/admin/logs
        [HttpGet("logs")]
        [ProducesResponseType(typeof(List<AdminLogDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdminLogs([FromQuery] LogFilterDto? filter)
        {
            filter ??= new LogFilterDto();
            var logs = await _adminService.GetAdminLogsAsync(filter);
            return Ok(logs);
        }

        // GET: api/admin/logs/export
        [HttpGet("logs/export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportLogs([FromQuery] LogFilterDto? filter)
        {
            var file = await _adminService.ExportLogsAsync(filter);
            return File(file, "text/csv", $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // ========== System Stats ==========
        
        // GET: api/admin/stats
        [HttpGet("stats")]
        [ProducesResponseType(typeof(SystemStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSystemStats()
        {
            var stats = await _adminService.GetSystemStatsAsync();
            return Ok(stats);
        }

        // POST: api/admin/clear-cache
        [HttpPost("clear-cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearCache()
        {
            var adminId = GetAdminId();
            await _adminService.ClearCacheAsync(adminId);
            return Ok(new { message = "تم مسح الذاكرة المؤقتة بنجاح" });
        }
    }
}