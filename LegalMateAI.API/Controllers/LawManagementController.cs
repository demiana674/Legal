using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;


namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LawManagementController : ControllerBase
    {
        private readonly ILawManagementService _lawManagementService;
        private readonly ILogger<LawManagementController> _logger;

        public LawManagementController(ILawManagementService lawManagementService, ILogger<LawManagementController> logger)
        {
            _lawManagementService = lawManagementService;
            _logger = logger;
        }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");

            if (claim != null && Guid.TryParse(claim.Value, out var userId))
                return userId;

            return null;            
        }

        

        /// <summary>
        /// إنشاء قانون جديد 
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddLaw([FromForm] CreateLawDto request)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var addedLawId = await _lawManagementService.AddLawAsync(adminId.Value, request);
            if (addedLawId == null)
                return BadRequest(new { message = "فشل إنشاء القانون" });

            return Ok(new { success = true, message = "تم إنشاء القانون بنجاح", lawId = addedLawId});
        }


        /// <summary>
        /// حذف قانون
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> RemoveLaw(Guid id)
        {
            var adminId = GetUserId();
            if (!adminId.HasValue)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var result = await _lawManagementService.RemoveLawAsync(adminId.Value, id);
            if (!result)
                return NotFound(new { message = "القانون غير موجود" });

            return Ok(new { success = true, message = "تم حذف القانون بنجاح" });
        }



    }


}