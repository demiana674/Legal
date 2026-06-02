// LegalMateAI.API/Controllers/LawyerBranchController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.DTOs.ReadDTO;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LawyerBranchController : ControllerBase
    {
        private readonly ILawyerBranchService _branchService;
        private readonly LegalMateDbContext _context;
        private readonly ILogger<LawyerBranchController> _logger;

        public LawyerBranchController(
            ILawyerBranchService branchService,
            LegalMateDbContext context,
            ILogger<LawyerBranchController> logger)
        {
            _branchService = branchService;
            _context = context;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? User.FindFirst("sub")?.Value;
            return Guid.Parse(userIdClaim!);
        }

        // جلب جميع فروع المحامي الحالي
        [Authorize(Roles = "Lawyer")]
        [HttpGet("branches")]
        public async Task<IActionResult> GetMyBranches()
        {
            var userId = GetUserId();
            var branches = await _branchService.GetLawyerBranchesAsync(userId);
            return Ok(branches);
        }

        // جلب فروع محامي معين (للعرض العام)
        [AllowAnonymous]
        [HttpGet("lawyer/{userId}")]
        public async Task<IActionResult> GetLawyerBranches(Guid userId)
        {
            var branches = await _branchService.GetLawyerBranchesAsync(userId);
            return Ok(branches);
        }

        // إنشاء فرع جديد
        [Authorize(Roles = "Lawyer")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateBranch([FromBody] CreateLawyerBranchDto request)
        {
            var userId = GetUserId();
            var branch = await _branchService.CreateBranchAsync(userId, request);
            
            if (branch == null)
                return BadRequest(new { message = "فشل إنشاء الفرع" });

            return Ok(new { success = true, message = "تم إنشاء الفرع بنجاح", data = branch });
        }

        // تحديث فرع
        [Authorize(Roles = "Lawyer")]
        [HttpPut("update/{branchId}")]
        public async Task<IActionResult> UpdateBranch(Guid branchId, [FromBody] UpdateLawyerBranchDto request)
        {
            var userId = GetUserId();
            var branch = await _branchService.UpdateBranchAsync(userId, branchId, request);
            
            if (branch == null)
                return BadRequest(new { message = "فشل تحديث الفرع" });

            return Ok(new { success = true, message = "تم تحديث الفرع بنجاح", data = branch });
        }

        // حذف فرع
        [Authorize(Roles = "Lawyer")]
        [HttpDelete("delete/{branchId}")]
        public async Task<IActionResult> DeleteBranch(Guid branchId)
        {
            var userId = GetUserId();
            var result = await _branchService.DeleteBranchAsync(userId, branchId);
            
            if (!result)
                return BadRequest(new { message = "فشل حذف الفرع" });

            return Ok(new { success = true, message = "تم حذف الفرع بنجاح" });
        }

        // أوقات توفر فرع معين
        [AllowAnonymous]
        [HttpGet("{branchId}/availability")]
        public async Task<IActionResult> GetBranchAvailability(Guid branchId)
        {
            var availability = await _branchService.GetBranchAvailabilityAsync(branchId);
            return Ok(availability);
        }

        // تحديث أوقات التوفر
        [Authorize(Roles = "Lawyer")]
        [HttpPut("{branchId}/availability")]
        public async Task<IActionResult> UpdateAvailability(Guid branchId, [FromBody] List<CreateBranchAvailabilityDto> availabilities)
        {
            var userId = GetUserId();
            var result = await _branchService.UpdateBranchAvailabilityAsync(userId, branchId, availabilities);
            return Ok(new { success = true, message = "تم تحديث أوقات التوفر بنجاح" });
        }

        // المواعيد المتاحة في يوم معين
        [AllowAnonymous]
        [HttpGet("{branchId}/available-slots")]
        public async Task<IActionResult> GetAvailableSlots(Guid branchId, [FromQuery] DateTime date)
        {
            var slots = await _branchService.GetAvailableTimeSlotsAsync(branchId, date);
            return Ok(slots);
        }
    }
}