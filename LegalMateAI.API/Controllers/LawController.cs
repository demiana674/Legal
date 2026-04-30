// LegalMateAI.API/Controllers/LawController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.Domain.Enums;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/laws")]
    public class LawController : ControllerBase
    {
        private readonly ILawService _lawService;
        public LawController(ILawService lawService) { _lawService = lawService; }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id") ?? User.FindFirst("sub");
            return claim != null ? Guid.Parse(claim.Value) : null;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetLaws([FromQuery] LawCategory? category = null, [FromQuery] string? search = null)
        {
            return Ok(await _lawService.GetLawsAsync(category, search));
        }

        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<IActionResult> SearchLaws([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { message = "يرجى إدخال كلمة البحث" });
            return Ok(await _lawService.SearchLawsAsync(q));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLaw(Guid id)
        {
            var law = await _lawService.GetLawByIdAsync(id);
            return law == null ? NotFound(new { message = "القانون غير موجود" }) : Ok(law);
        }

        [AllowAnonymous]
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadLaw(Guid id)
        {
            var fileBytes = await _lawService.DownloadLawAsync(id);
            if (fileBytes != null && fileBytes.Length > 0)
            {
                var law = await _lawService.GetLawByIdAsync(id);
                return File(fileBytes, "application/pdf", $"{law?.Name ?? "law"}.pdf");
            }
            var downloadUrl = await _lawService.GetLawDownloadUrlAsync(id);
            if (!string.IsNullOrEmpty(downloadUrl)) return Redirect(downloadUrl);
            return NotFound(new { message = "الملف غير متوفر" });
        }

        [AllowAnonymous]
        [HttpGet("{id}/download-info")]
        public async Task<IActionResult> GetDownloadInfo(Guid id)
        {
            var info = await _lawService.GetLawDownloadInfoAsync(id);
            return info == null ? NotFound(new { message = "القانون غير موجود" }) : Ok(info);
        }

        [AllowAnonymous]
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories() => Ok(await _lawService.GetLawCategoriesAsync());
    }
}