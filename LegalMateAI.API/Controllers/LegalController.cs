// LegalMateAI.API/Controllers/LegalController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LegalController : ControllerBase
    {
        private readonly ILegalService _legalService;

        public LegalController(ILegalService legalService)
        {
            _legalService = legalService;
        }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? Guid.Parse(claim.Value) : (Guid?)null;
        }

        // GET: api/legal/search
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LawSearchResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchLaws([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "يرجى إدخال نص البحث" });

            var result = await _legalService.SearchLawsAsync(q, page, pageSize);

            // حفظ البحث إذا كان المستخدم مسجل دخول
            if (GetUserId().HasValue)
            {
                await _legalService.SaveSearchQueryAsync(GetUserId().Value, q, result.TotalResults);
            }

            return Ok(result);
        }

        // POST: api/legal/smart-search
        [HttpPost("smart-search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LawSearchResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SmartSearch([FromBody] SmartSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { message = "يرجى إدخال نص البحث" });

            var result = await _legalService.SmartSearchAsync(request.Query, request.Page, request.PageSize);

            // حفظ البحث إذا كان المستخدم مسجل دخول
            if (GetUserId().HasValue)
            {
                await _legalService.SaveSearchQueryAsync(GetUserId().Value, request.Query, result.TotalResults);
            }

            return Ok(result);
        }

        // GET: api/legal/laws
        [HttpGet("laws")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<EgyptianLawResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllLaws([FromQuery] LawCategory? category)
        {
            var laws = await _legalService.GetAllLawsAsync(category);
            return Ok(laws);
        }

        // GET: api/legal/laws/{id}
        [HttpGet("laws/{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EgyptianLawResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLawById(int id)
        {
            var law = await _legalService.GetLawByIdAsync(id);

            if (law == null)
                return NotFound(new { message = "القانون غير موجود" });

            return Ok(law);
        }

        // GET: api/legal/articles/{id}
        [HttpGet("articles/{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LawArticleDetailedDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetArticleById(int id)
        {
            var article = await _legalService.GetArticleByIdAsync(id);

            if (article == null)
                return NotFound(new { message = "المادة القانونية غير موجودة" });

            return Ok(article);
        }

        // GET: api/legal/laws/{lawId}/amendments
        [HttpGet("laws/{lawId}/amendments")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<LawAmendmentBriefDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLawAmendments(int lawId)
        {
            var amendments = await _legalService.GetLawAmendmentsAsync(lawId);
            return Ok(amendments);
        }

        // GET: api/legal/articles/{articleId}/interpretations
        [HttpGet("articles/{articleId}/interpretations")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<LawInterpretationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetArticleInterpretations(int articleId)
        {
            var interpretations = await _legalService.GetArticleInterpretationsAsync(articleId);
            return Ok(interpretations);
        }

        // GET: api/legal/history
        [Authorize]
        [HttpGet("history")]
        [ProducesResponseType(typeof(List<SearchQueryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSearchHistory()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var history = await _legalService.GetUserSearchHistoryAsync(userId.Value);
            return Ok(history);
        }
    }

    public class SmartSearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}