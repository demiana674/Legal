// LegalMateAI.API/Controllers/LawyerBranchController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/lawyer/branches")]
    public class LawyerBranchController : ControllerBase
    {
        private readonly ILawyerBranchService _branchService;
        private readonly ILogger<LawyerBranchController> _logger;

        public LawyerBranchController(ILawyerBranchService branchService, ILogger<LawyerBranchController> logger)
        {
            _branchService = branchService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!.Value);
        }

        // ========== للجميع ==========

        /// <summary>
        /// ✅ فروع محامي معين
        /// </summary>
        [AllowAnonymous]
        [HttpGet("lawyer/{lawyerId}")]
        public async Task<IActionResult> GetLawyerBranches(Guid lawyerId)
        {
            var branches = await _branchService.GetLawyerBranchesAsync(lawyerId);
            return Ok(branches);
        }

        /// <summary>
        /// ✅ أوقات توفر فرع معين
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{branchId}/availability")]
        public async Task<IActionResult> GetBranchAvailability(Guid branchId)
        {
            var availability = await _branchService.GetBranchAvailabilityAsync(branchId);
            return Ok(availability);
        }

        /// <summary>
        /// ✅ المواعيد المتاحة في يوم معين
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{branchId}/available-slots")]
        public async Task<IActionResult> GetAvailableSlots(Guid branchId, [FromQuery] DateTime date)
        {
            var slots = await _branchService.GetAvailableTimeSlotsAsync(branchId, date);
            return Ok(slots);
        }

        // ========== للمحامي فقط ==========

        /// <summary>
        /// ✅ إضافة فرع جديد
        /// </summary>
        [Authorize(Roles = "Lawyer")]
        [HttpPost]
        public async Task<IActionResult> CreateBranch([FromBody] CreateLawyerBranchDto request)
        {
            var lawyerId = GetUserId();
            var result = await _branchService.CreateBranchAsync(lawyerId, request);
            return Ok(result);
        }

        /// <summary>
        /// ✅ تعديل فرع
        /// </summary>
        [Authorize(Roles = "Lawyer")]
        [HttpPut("{branchId}")]
        public async Task<IActionResult> UpdateBranch(Guid branchId, [FromBody] UpdateLawyerBranchDto request)
        {
            var lawyerId = GetUserId();
            var result = await _branchService.UpdateBranchAsync(lawyerId, branchId, request);
            return Ok(result);
        }

        /// <summary>
        /// ✅ حذف فرع
        /// </summary>
        [Authorize(Roles = "Lawyer")]
        [HttpDelete("{branchId}")]
        public async Task<IActionResult> DeleteBranch(Guid branchId)
        {
            var lawyerId = GetUserId();
            var result = await _branchService.DeleteBranchAsync(lawyerId, branchId);
            return Ok(new { message = "تم حذف الفرع بنجاح" });
        }

        /// <summary>
        /// ✅ تحديث أوقات التوفر لفرع (المحامي يضيف أيام وساعات العمل)
        /// </summary>
        [Authorize(Roles = "Lawyer")]
        [HttpPut("{branchId}/availability")]
        public async Task<IActionResult> UpdateAvailability(Guid branchId, [FromBody] List<CreateBranchAvailabilityDto> availabilities)
        {
            var lawyerId = GetUserId();
            var result = await _branchService.UpdateBranchAvailabilityAsync(lawyerId, branchId, availabilities);
            return Ok(new { message = "تم تحديث أوقات التوفر بنجاح" });
        }
    }
}