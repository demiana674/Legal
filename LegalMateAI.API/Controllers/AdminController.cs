// LegalMateAI.API/Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // ==================== Dashboard ====================

        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var adminId = GetCurrentUserId();
            if (!adminId.HasValue) return Unauthorized(new { message = "لم يتم العثور على معرف المدير" });

            var stats = await _adminService.GetDashboardStatsAsync(adminId.Value);
            return Ok(stats);
        }

        // ==================== User Management ====================

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterDto? filter = null)
        {
            var users = await _adminService.GetAllUsersAsync(filter);
            return Ok(users);
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserDetails(Guid userId)
        {
            var user = await _adminService.GetUserDetailsAsync(userId);
            if (user == null) return NotFound(new { message = "المستخدم غير موجود" });
            return Ok(user);
        }

        [HttpPut("users/{userId}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusDto dto)
        {
            if (dto == null) return BadRequest(new { message = "البيانات المطلوبة غير موجودة" });

            var adminId = GetCurrentUserId();
            if (!adminId.HasValue) return Unauthorized(new { message = "لم يتم العثور على معرف المدير" });

            var result = await _adminService.UpdateUserStatusAsync(adminId.Value, userId, dto.Status, dto.Reason);
            if (!result) return NotFound(new { message = "المستخدم غير موجود" });

            return Ok(new { message = "تم تحديث حالة المستخدم بنجاح" });
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var adminId = GetCurrentUserId();
            if (!adminId.HasValue) return Unauthorized(new { message = "لم يتم العثور على معرف المدير" });

            var result = await _adminService.DeleteUserAsync(adminId.Value, userId);
            if (!result) return NotFound(new { message = "المستخدم غير موجود" });

            return Ok(new { message = "تم حذف المستخدم بنجاح" });
        }

        // ==================== Lawyer Management ====================

        [HttpGet("lawyers/pending")]
        public async Task<IActionResult> GetPendingLawyers()
        {
            var lawyers = await _adminService.GetPendingLawyersAsync();
            return Ok(lawyers);
        }

        [HttpGet("lawyers")]
        public async Task<IActionResult> GetAllLawyers([FromQuery] LawyerFilterDto? filter = null)
        {
            var lawyers = await _adminService.GetAllLawyersAsync(filter);
            return Ok(lawyers);
        }

        [HttpGet("lawyers/{lawyerId}")]
        public async Task<IActionResult> GetLawyerDetails(Guid lawyerId)
        {
            var lawyer = await _adminService.GetLawyerDetailsAsync(lawyerId);
            if (lawyer == null) return NotFound(new { message = "المحامي غير موجود" });
            return Ok(lawyer);
        }

        [HttpPost("lawyers/{lawyerId}/approve")]
        public async Task<IActionResult> ApproveLawyer(Guid lawyerId)
        {
            var result = await _adminService.ApproveLawyerAsync(lawyerId);
            if (!result) return NotFound(new { message = "المحامي غير موجود" });
            return Ok(new { message = "تم قبول المحامي بنجاح" });
        }

        [HttpPost("lawyers/{lawyerId}/reject")]
        public async Task<IActionResult> RejectLawyer(Guid lawyerId, [FromBody] string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return BadRequest(new { message = "يجب تقديم سبب الرفض" });

            var result = await _adminService.RejectLawyerAsync(lawyerId, reason);
            if (!result) return NotFound(new { message = "المحامي غير موجود" });
            return Ok(new { message = "تم رفض المحامي", reason });
        }

        [HttpPost("lawyers/{lawyerId}/suspend")]
        public async Task<IActionResult> SuspendLawyer(Guid lawyerId, [FromBody] string? reason = null)
        {
            var result = await _adminService.SuspendLawyerAsync(lawyerId, reason);
            if (!result) return NotFound(new { message = "المحامي غير موجود" });
            return Ok(new { message = "تم تعليق المحامي", reason });
        }

        [HttpPost("lawyers/{lawyerId}/activate")]
        public async Task<IActionResult> ActivateLawyer(Guid lawyerId)
        {
            var result = await _adminService.ActivateLawyerAsync(lawyerId);
            if (!result) return NotFound(new { message = "المحامي غير موجود" });
            return Ok(new { message = "تم تفعيل المحامي بنجاح" });
        }

        [HttpDelete("lawyers/{lawyerId}")]
        public async Task<IActionResult> DeleteLawyer(Guid lawyerId)
        {
            var result = await _adminService.DeleteLawyerAsync(lawyerId);
            if (!result) return NotFound(new { message = "المحامي غير موجود" });
            return Ok(new { message = "تم حذف المحامي بنجاح" });
        }

        // ==================== Log Management ====================

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] LogFilterDto? filter = null)
        {
            var result = await _adminService.GetAllLogsAsync(filter ?? new LogFilterDto());
            return Ok(result);
        }

        // [HttpGet("users/logs")]
        // public async Task<IActionResult> GetUserLogs([FromQuery] LogFilterDto? filter = null)
        // {
        //     var result = await _adminService.GetAllLogsAsync(filter ?? new LogFilterDto());
        //     return Ok(result);
        // }

        [HttpGet("logs/stats")]
        public async Task<IActionResult> GetLogsStats()
        {
            var stats = await _adminService.GetLogsStatsAsync();
            return Ok(stats);
        }

        // ==================== Export Methods ====================

//         [HttpGet("export/csv")]
// public async Task<IActionResult> ExportLogsToCsv([FromQuery] LogFilterDto? filter = null)
// {
//     var bytes = await _adminService.ExportLogsAsync(filter ?? new LogFilterDto(), "csv");

//     return File(
//         bytes,
//         "text/csv",
//         $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
//     );
// }

[HttpGet("export/excel")]
public async Task<IActionResult> ExportLogsToExcel([FromQuery] LogFilterDto? filter = null)
{
    var bytes = await _adminService.ExportLogsAsync(filter ?? new LogFilterDto(), "excel");

    return File(
        bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
    );
}

[HttpGet("export/pdf")]
public async Task<IActionResult> ExportLogsToPdf([FromQuery] LogFilterDto? filter = null)
{
    var bytes = await _adminService.ExportLogsToPdfAsync(filter ?? new LogFilterDto());

    return File(
        bytes,
        "application/pdf",
        $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
    );
}

        // ==================== System Management ====================

        [HttpGet("system/stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            var stats = await _adminService.GetSystemStatsAsync();
            return Ok(stats);
        }

        [HttpPost("system/clear-cache")]
        public async Task<IActionResult> ClearCache()
        {
            var adminId = GetCurrentUserId();
            if (!adminId.HasValue) return Unauthorized(new { message = "لم يتم العثور على معرف المدير" });

            var result = await _adminService.ClearCacheAsync(adminId.Value);
            return Ok(new { message = "تم مسح الكاش بنجاح" });
        }

        // ==================== Helper Methods ====================

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("id")
                ?? User.FindFirst("sub");

            if (userIdClaim == null) return null;

            return Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
        }
    }
}