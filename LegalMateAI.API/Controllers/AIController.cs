using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.ReadDTO;
using System.Security.Claims;
using System.Text;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIController> _logger;

        public AIController(IAIService aiService, ILogger<AIController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }

        private string GetSessionId()
        {
            if (Request.Cookies.TryGetValue("chat_session_id", out var sessionId))
                return sessionId;
            
            sessionId = Guid.NewGuid().ToString();
            Response.Cookies.Append("chat_session_id", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            });
            return sessionId;
        }

        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            var isHealthy = await _aiService.HealthCheckAsync();
            return Ok(new
            {
                status = isHealthy ? "healthy" : "unhealthy",
                service = "Gemini AI",
                timestamp = DateTime.UtcNow
            });
        }

        [Authorize]
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeDocument([FromBody] AnalyzeDocumentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { message = "النص مطلوب" });

            var userId = GetUserId();
            
            var document = new Document
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = "text-input.txt"
            };

            var content = Encoding.UTF8.GetBytes(request.Text);
            var result = await _aiService.AnalyzeDocumentAsync(document, content);

            return Ok(result);
        }

        [Authorize]
        [HttpPost("analyze/file")]
        public async Task<IActionResult> AnalyzeFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "الملف مطلوب" });

            var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".txt" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = "نوع الملف غير مدعوم" });

            if (file.Length > 50 * 1024 * 1024)
                return BadRequest(new { message = "حجم الملف يتجاوز 50 ميجابايت" });

            var userId = GetUserId();
            
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();

            var document = new Document
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = file.FileName,
                FileType = file.ContentType,
                FileSize = file.Length
            };

            var result = await _aiService.AnalyzeDocumentAsync(document, fileContent);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("analyze/quick")]
        public async Task<IActionResult> QuickAnalysis([FromBody] QuickAnalysisRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { message = "النص مطلوب" });

            var result = await _aiService.QuickAnalysisAsync(request.Text);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("generate-contract")]
        public async Task<IActionResult> GenerateContract([FromBody] AIGenerateContractRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TemplateName))
                return BadRequest(new { message = "اسم القالب مطلوب" });

            var template = new ContractTemplate
            {
                Id = Guid.NewGuid(),
                Name = request.TemplateName,
                Type = Domain.Enums.ContractType.Other
            };

            var result = await _aiService.GenerateContractAsync(template, request.Data);
            return Ok(new { content = result });
        }
    }

    #region Request Models

    public class AnalyzeDocumentRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class QuickAnalysisRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class AIGenerateContractRequest
    {
        public string TemplateName { get; set; } = string.Empty;
        public Dictionary<string, string> Data { get; set; } = new();
    }

    #endregion
}