using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LawyerController : ControllerBase
    {
        private readonly ILawyerService _lawyerService;
        private readonly ILogger<LawyerController> _logger;

        public LawyerController(
            ILawyerService lawyerService,
            ILogger<LawyerController> logger)
        {
            _lawyerService = lawyerService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) claim = User.FindFirst("id");
            if (claim == null) claim = User.FindFirst("sub");
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }

        /// <summary>
        /// ✅ جلب جميع تخصصات المحامين
        /// </summary>
        [AllowAnonymous]
        [HttpGet("specialties")]
        [ProducesResponseType(typeof(List<LawyerSpecialtyResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSpecialties()
        {
            var specialties = await _lawyerService.GetSpecialtiesAsync();
            return Ok(specialties);
        }

        /// <summary>
        /// ✅ بحث عن محامين (يدعم فلترة متقدمة)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<LawyerResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchLawyers([FromQuery] LawyerSearchDto searchCriteria)
        {
            var lawyers = await _lawyerService.SearchLawyersAsync(searchCriteria);
            
            if (!lawyers.Any())
                return Ok(new { message = "لا يوجد محامين متاحين حالياً", lawyers = new List<LawyerResponseDto>() });
            
            return Ok(new { message = $"تم العثور على {lawyers.Count} محامي", lawyers });
        }

        /// <summary>
        /// ✅ جلب محامي محدد بالـ ID
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{lawyerId:guid}")]
        [ProducesResponseType(typeof(LawyerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLawyerById(Guid lawyerId)
        {
            _logger.LogInformation($"Fetching lawyer by ID: {lawyerId}");
            
            var lawyer = await _lawyerService.GetLawyerByIdAsync(lawyerId);
            
            if (lawyer == null)
                return NotFound(new { message = "المحامي غير موجود" });
            
            return Ok(lawyer);
        }

        /// <summary>
        /// ✅ جلب تقييمات محامي
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{lawyerId:guid}/reviews")]
        [ProducesResponseType(typeof(List<ReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLawyerReviews(Guid lawyerId)
        {
            _logger.LogInformation($"Fetching reviews for lawyer ID: {lawyerId}");
            
            var lawyer = await _lawyerService.GetLawyerByIdAsync(lawyerId);
            if (lawyer == null)
                return NotFound(new { message = "المحامي غير موجود" });

            var reviews = await _lawyerService.GetLawyerReviewsAsync(lawyer.Id);
            
            return Ok(new 
            { 
                lawyerName = lawyer.FullName,
                averageRating = lawyer.Rating,
                totalReviews = reviews.Count,
                reviews 
            });
        }

        /// <summary>
        /// ✅ إضافة تقييم لمحامي (يحتاج تسجيل دخول)
        /// </summary>
        [Authorize]
        [HttpPost("{lawyerId:guid}/reviews")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddReview(Guid lawyerId, [FromBody] AddReviewRequest request)
        {
            var userId = GetUserId();
            
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "يجب تسجيل الدخول لإضافة تقييم" });

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { message = "التقييم يجب أن يكون بين 1 و 5" });

            var lawyer = await _lawyerService.GetLawyerByIdAsync(lawyerId);
            if (lawyer == null)
                return NotFound(new { message = "المحامي غير موجود" });

            var result = await _lawyerService.AddReviewAsync(
                userId, 
                lawyer.Id, 
                request.Rating, 
                request.Comment, 
                request.AppointmentId);

            if (!result)
                return BadRequest(new { message = "لقد قمت بتقييم هذا المحامي من قبل" });

            _logger.LogInformation($"User {userId} added review for lawyer {lawyerId}");
            
            return Ok(new { message = "تم إضافة التقييم بنجاح" });
        }

        /// <summary>
        /// ✅ جلب محامين حسب التخصص
        /// </summary>
        [AllowAnonymous]
        [HttpGet("specialization/{specialization}")]
        [ProducesResponseType(typeof(List<LawyerResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLawyersBySpecialization(
            string specialization, 
            [FromQuery] int limit = 5)
        {
            var lawyers = await _lawyerService.GetLawyersBySpecializationAsync(specialization, limit);
            
            if (!lawyers.Any())
                return Ok(new { message = "لا يوجد محامين في هذا التخصص حالياً", lawyers = new List<LawyerResponseDto>() });
            
            return Ok(new 
            { 
                specialization,
                count = lawyers.Count,
                lawyers 
            });
        }

        /// <summary>
        /// ✅ جلب أوقات توفر المحامي
        /// </summary>
        [Authorize(Roles = "Lawyer")]
        [HttpGet("availability")]
        [ProducesResponseType(typeof(List<AvailabilityDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailability()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            var availability = await _lawyerService.GetLawyerAvailabilityAsync(userId);
            return Ok(availability);
        }

        /// <summary>
        /// ✅ تحديث أوقات توفر المحامي
        /// </summary>
        [Authorize(Roles = "Lawyer")]
        [HttpPut("availability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAvailability([FromBody] List<CreateLawyerAvailabilityDto> availabilities)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "يجب تسجيل الدخول" });

            if (availabilities == null || !availabilities.Any())
                return BadRequest(new { message = "بيانات أوقات التوفر مطلوبة" });

            var result = await _lawyerService.UpdateAvailabilityAsync(userId, availabilities);
            if (!result)
                return BadRequest(new { message = "فشل تحديث أوقات التوفر" });

            return Ok(new { message = "تم تحديث أوقات التوفر بنجاح" });
        }
    }
}