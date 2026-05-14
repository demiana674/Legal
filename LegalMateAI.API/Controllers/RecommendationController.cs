using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;
        private readonly ILogger<RecommendationController> _logger;

        public RecommendationController(
            IRecommendationService recommendationService,
            ILogger<RecommendationController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("id")
                ?? User.FindFirst("sub");
            
            if (claim == null)
                throw new UnauthorizedAccessException("User ID not found");
            
            return Guid.Parse(claim.Value);
        }

        [HttpPost("from-document/{documentId}")]
        public async Task<IActionResult> RecommendFromDocument(Guid documentId, [FromQuery] int topK = 5)
        {
            var userId = GetUserId();
            
            var analysisText = await _recommendationService.GetDocumentAnalysisAsync(documentId);
            
            if (string.IsNullOrEmpty(analysisText))
                return BadRequest(new { message = "المستند غير موجود أو لم يتم تحليله بعد" });
            
            var recommendations = await _recommendationService.RecommendLawyersAsync(
                userId: userId,
                documentAnalysisText: analysisText,
                topK: topK);
            
            return Ok(new
            {
                success = true,
                message = $"تم العثور على {recommendations.Count} محامي موصى به",
                recommendations,
                basedOn = "تحليل المستند"
            });
        }

        [HttpPost("from-case-description")]
        public async Task<IActionResult> RecommendFromDescription(
            [FromBody] RecommendFromDescriptionRequest request,
            [FromQuery] int topK = 5)
        {
            var userId = GetUserId();
            
            if (string.IsNullOrWhiteSpace(request.Description))
                return BadRequest(new { message = "وصف القضية مطلوب" });
            
            var recommendations = await _recommendationService.RecommendLawyersAsync(
                userId: userId,
                caseDescription: request.Description,
                caseType: request.CaseType,
                governorateId: request.GovernorateId,
                topK: topK);
            
            return Ok(new
            {
                success = true,
                message = $"تم العثور على {recommendations.Count} محامي موصى به",
                recommendations,
                basedOn = "وصف القضية"
            });
        }

        [HttpGet("for-me")]
        public async Task<IActionResult> RecommendForMe([FromQuery] int topK = 5)
        {
            var userId = GetUserId();
            
            var recommendations = await _recommendationService.RecommendByUserHistoryAsync(userId, topK);
            
            return Ok(new
            {
                success = true,
                message = recommendations.Any() 
                    ? $"تم العثور على {recommendations.Count} محامي موصى به لك"
                    : "لا توجد توصيات حالياً، يرجى تقييم بعض المحامين أولاً",
                recommendations,
                basedOn = "سجل التقييمات السابقة"
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("train")]
        public async Task<IActionResult> TrainModel()
        {
            _logger.LogInformation($"Admin {GetUserId()} triggered model training");
            
            var result = await _recommendationService.TrainRecommendationModelAsync();
            
            return Ok(new
            {
                success = result,
                message = result ? "تم تدريب نموذج التوصية بنجاح" : "فشل تدريب النموذج",
                timestamp = DateTime.UtcNow
            });
        }
    }

    public class RecommendFromDescriptionRequest
    {
        public string Description { get; set; } = string.Empty;
        public string? CaseType { get; set; }
        public int? GovernorateId { get; set; }
    }
}