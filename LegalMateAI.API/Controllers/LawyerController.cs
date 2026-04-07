// LegalMateAI.API/Controllers/LawyerController.cs
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

        public LawyerController(ILawyerService lawyerService)
        {
            _lawyerService = lawyerService;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }

        // GET: api/lawyer/search
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<LawyerResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchLawyers([FromQuery] LawyerSearchDto searchCriteria)
        {
            var lawyers = await _lawyerService.SearchLawyersAsync(searchCriteria);
            return Ok(lawyers);
        }

        // GET: api/lawyer/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LawyerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLawyerById(Guid id)
        {
            var lawyer = await _lawyerService.GetLawyerByIdAsync(id);

            if (lawyer == null)
                return NotFound(new { message = "المحامي غير موجود" });

            return Ok(lawyer);
        }

        // GET: api/lawyer/{id}/availability
        [HttpGet("{id}/availability")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<AvailabilityDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLawyerAvailability(Guid id)
        {
            var availability = await _lawyerService.GetLawyerAvailabilityAsync(id);
            return Ok(availability);
        }

        // PUT: api/lawyer/availability
        [Authorize(Roles = "Lawyer")]
        [HttpPut("availability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAvailability([FromBody] List<CreateLawyerAvailabilityDto> availabilities)
        {
            var lawyerId = GetUserId();
            var result = await _lawyerService.UpdateAvailabilityAsync(lawyerId, availabilities);

            if (!result)
                return BadRequest(new { message = "فشل تحديث أوقات التوفر" });

            return Ok(new { message = "تم تحديث أوقات التوفر بنجاح" });
        }

        // // GET: api/lawyer/specializations
        // [HttpGet("specializations")]
        // [AllowAnonymous]
        // [ProducesResponseType(typeof(List<LegalSpecializationDto>), StatusCodes.Status200OK)]
        // public async Task<IActionResult> GetSpecializations()
        // {
        //     var specializations = await _lawyerService.GetSpecializationsAsync();
        //     return Ok(specializations);
        // }

        // GET: api/lawyer/by-specialization
        [HttpGet("by-specialization")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<LawyerResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLawyersBySpecialization([FromQuery] string specialization, [FromQuery] int limit = 5)
        {
            var lawyers = await _lawyerService.GetLawyersBySpecializationAsync(specialization, limit);
            return Ok(lawyers);
        }


        [HttpGet("specialties")]
[AllowAnonymous]
[ProducesResponseType(typeof(List<LawyerSpecialtyResponseDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetSpecialties()
{
    var specialties = await _lawyerService.GetSpecialtiesAsync();
    return Ok(specialties);
}

        // GET: api/lawyer/{id}/reviews
        [HttpGet("{id}/reviews")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<ReviewDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLawyerReviews(Guid id)
        {
            var reviews = await _lawyerService.GetLawyerReviewsAsync(id);
            return Ok(reviews);
        }

        // POST: api/lawyer/{id}/review
        [Authorize(Roles = "User")]
        [HttpPost("{id}/review")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddReview(Guid id, [FromBody] AddReviewRequest request)
        {
            var userId = GetUserId();

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { message = "التقييم يجب أن يكون بين 1 و 5" });

            var result = await _lawyerService.AddReviewAsync(userId, id, request.Rating, request.Comment, request.AppointmentId);

            if (!result)
                return BadRequest(new { message = "لا يمكن إضافة تقييم. تأكد من وجود موعد مكتمل مع هذا المحامي" });

            return Ok(new { message = "تم إضافة التقييم بنجاح" });
        }
    }

    public class AddReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public Guid? AppointmentId { get; set; }
    }
}