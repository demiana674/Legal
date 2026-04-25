// LegalMateAI.API/Controllers/AIController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/AI")]
    public class AIController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IAIService _aiService;
        private readonly ILogger<AIController> _logger;

        public AIController(
            IChatService chatService,
            IAIService aiService,
            ILogger<AIController> logger)
        {
            _chatService = chatService;
            _aiService = aiService;
            _logger = logger;
        }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("id")
                ?? User.FindFirst("sub");
            return claim != null ? Guid.Parse(claim.Value) : null;
        }

        /// <summary>
        /// محادثة مع المساعد الذكي (متاح للجميع)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("conversation")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto request)
        {
            var userId = GetUserId();
            var response = await _chatService.SendMessageAsync(userId, request);

            if (response == null)
                return StatusCode(500, new { message = "فشل في الحصول على رد من المساعد" });

            return Ok(new { success = true, data = response });
        }

        /// <summary>
        /// جلب تاريخ المحادثة
        /// </summary>
        [AllowAnonymous]
        [HttpGet("conversation/history")]
        public async Task<IActionResult> GetHistory([FromQuery] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { message = "معرف الجلسة مطلوب" });

            var userId = GetUserId();
            var history = await _chatService.GetConversationHistoryAsync(userId, sessionId);

            if (history == null)
                return Ok(new
                {
                    success = true,
                    data = new ConversationHistoryDto
                    {
                        SessionId = sessionId,
                        Messages = new(),
                        MessageCount = 0
                    }
                });

            return Ok(new { success = true, data = history });
        }

        /// <summary>
        /// مسح تاريخ المحادثة
        /// </summary>
        [AllowAnonymous]
        [HttpDelete("conversation/clear")]
        public async Task<IActionResult> ClearHistory([FromQuery] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { message = "معرف الجلسة مطلوب" });

            var userId = GetUserId();
            var result = await _chatService.ClearConversationHistoryAsync(userId, sessionId);

            return Ok(new
            {
                success = true,
                message = result ? "تم مسح التاريخ" : "لا يوجد تاريخ للمسح"
            });
        }

        /// <summary>
        /// رد سريع (بدون حفظ)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("quick")]
        public async Task<IActionResult> QuickResponse([FromBody] QuickChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "الرسالة مطلوبة" });

            var response = await _chatService.GetQuickResponseAsync(request.Message);

            return Ok(new { success = true, response });
        }

        /// <summary>
        /// التحقق من حالة الخدمة
        /// </summary>
        [AllowAnonymous]
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            var isHealthy = await _aiService.HealthCheckAsync();
            return Ok(new { status = isHealthy ? "healthy" : "unavailable", service = "Gemini AI" });
        }
    }

    public class QuickChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}