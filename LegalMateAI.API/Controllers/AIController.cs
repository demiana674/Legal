using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;

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

        /// <summary>
        /// ✅ فحص حالة الخدمة
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            var isHealthy = await _aiService.HealthCheckAsync();
            return Ok(new
            {
                status = isHealthy ? "healthy" : "unhealthy",
                service = "Local AI (Ollama/LM Studio)",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// ✅ شات بوت - محادثة مباشرة (متاح للجميع)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "الرسالة مطلوبة" });

            var response = await _aiService.ChatAsync(request.Message);

            return Ok(new
            {
                message = response,
                timestamp = DateTime.UtcNow
            });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}