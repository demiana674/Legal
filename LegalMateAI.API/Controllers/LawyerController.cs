using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.Infrastructure.Services.IService;
using BCrypt.Net;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LawyerController : ControllerBase
    {
        private readonly ILawyerService _lawyerService;
        private readonly ILogger<LawyerController> _logger;
        private readonly LegalMateDbContext _context;
        private readonly IEncryptionService _encryption;

        public LawyerController(
            ILawyerService lawyerService,
            ILogger<LawyerController> logger,
            LegalMateDbContext context,
            IEncryptionService encryption)
        {
            _lawyerService = lawyerService;
            _logger = logger;
            _context = context;
            _encryption = encryption;
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
        /// ✅ جلب قائمة المهارات المتاحة للمحامين
        /// </summary>
        [AllowAnonymous]
        [HttpGet("available-skills")]
        [ProducesResponseType(typeof(List<LawyerSkillResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableSkills([FromQuery] string? category = null)
        {
            var query = _context.Set<LawyerSkill>()
                .Where(s => s.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(s => s.Category == category);
            }

            var skills = await query
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.NameAr)
                .Select(s => new LawyerSkillResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    NameAr = s.NameAr,
                    Description = s.Description,
                    Icon = s.Icon,
                    Category = s.Category
                })
                .ToListAsync();

            return Ok(skills);
        }

        /// <summary>
        /// ✅ جلب تصنيفات المهارات
        /// </summary>
        [AllowAnonymous]
        [HttpGet("available-skills/categories")]
        public async Task<IActionResult> GetSkillCategories()
        {
            var categories = await _context.Set<LawyerSkill>()
                .Where(s => s.IsActive && s.Category != null)
                .Select(s => s.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// ✅ بحث عن محامين
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
        /// ✅ إضافة تقييم لمحامي
        /// </summary>
        [Authorize]
        [HttpPost("{lawyerId:guid}/reviews")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        public async Task<IActionResult> GetLawyersBySpecialization(string specialization, [FromQuery] int limit = 5)
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

        /// <summary>
        /// ✅ إضافة تخصص لمحامي
        /// </summary>
        [Authorize(Roles = "Admin,Lawyer")]
        [HttpPost("{lawyerUserId}/add-specialty")]
        public async Task<IActionResult> AddSpecialtyToLawyer(Guid lawyerUserId, [FromBody] int specialtyId)
        {
            var currentUserId = GetUserId();
            
            if (currentUserId != lawyerUserId && !User.IsInRole("Admin"))
                return Forbid();
            
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == lawyerUserId && u.Role == UserRole.Lawyer);
            
            if (user?.LawyerProfile == null)
                return NotFound(new { message = "المحامي غير موجود" });
            
            var specialty = await _context.LawyerSpecialties
                .FirstOrDefaultAsync(s => s.Id == specialtyId && s.IsActive);
            
            if (specialty == null)
                return NotFound(new { message = "التخصص غير موجود" });
            
            var existing = await _context.LawyerSpecializations
                .FirstOrDefaultAsync(ls => ls.LawyerId == user.LawyerProfile.Id && ls.SpecializationId == specialtyId);
            
            if (existing != null)
                return BadRequest(new { message = "التخصص مضاف بالفعل" });
            
            var lawyerSpecialization = new LawyerSpecialization
            {
                Id = Guid.NewGuid(),
                LawyerId = user.LawyerProfile.Id,
                SpecializationId = specialtyId,
                IsPrimary = false,
                CasesCount = 0
            };
            
            _context.LawyerSpecializations.Add(lawyerSpecialization);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "تم إضافة التخصص بنجاح" });
        }

        /// <summary>
        /// ✅ حذف تخصص من محامي
        /// </summary>
        [Authorize(Roles = "Admin,Lawyer")]
        [HttpDelete("{lawyerUserId}/remove-specialty/{specialtyId}")]
        public async Task<IActionResult> RemoveSpecialtyFromLawyer(Guid lawyerUserId, int specialtyId)
        {
            var currentUserId = GetUserId();
            
            if (currentUserId != lawyerUserId && !User.IsInRole("Admin"))
                return Forbid();
            
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == lawyerUserId && u.Role == UserRole.Lawyer);
            
            if (user?.LawyerProfile == null)
                return NotFound(new { message = "المحامي غير موجود" });
            
            var existing = await _context.LawyerSpecializations
                .FirstOrDefaultAsync(ls => ls.LawyerId == user.LawyerProfile.Id && ls.SpecializationId == specialtyId);
            
            if (existing == null)
                return NotFound(new { message = "التخصص غير مضاف" });
            
            _context.LawyerSpecializations.Remove(existing);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "تم حذف التخصص بنجاح" });
        }

        // ==================== Clients Management ====================

        /// <summary>
        /// ✅ جلب جميع موكلين المحامي
        /// </summary>
        [Authorize(Roles = "Lawyer")]
        [HttpGet("my-clients")]
        public async Task<IActionResult> GetMyClients()
        {
            var userId = GetUserId();
            
            var lawyer = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == userId);
            
            if (lawyer == null)
                return NotFound(new { message = "الملف الشخصي غير موجود" });

            var clients = await _context.Cases
                .Where(c => c.LawyerId == lawyer.Id)
                .Include(c => c.Client)
                    .ThenInclude(c => c.UserProfile)
                .Include(c => c.Client)
                    .ThenInclude(c => c.Appointments)
                .Include(c => c.Client)
                    .ThenInclude(c => c.Contracts)
                .Select(c => new ClientResponseDto
                {
                    ClientId = c.Client.UserID,
                    ClientName = c.Client.FullName,
                    ClientInitials = c.Client.FullName.Length >= 2 ? c.Client.FullName.Substring(0, 2) : c.Client.FullName,
                    ClientEmail = c.Client.Email,
                    ClientPhone = _encryption.Decrypt(c.Client.Phone ?? ""),
                    ClientSince = c.Client.CreatedAt,
                    CaseId = c.Id,
                    CaseTitle = c.Title,
                    CaseType = c.CaseType ?? "غير محدد",
                    CaseNumber = c.CaseNumber,
                    CaseDescription = c.Description ?? "",
                    CaseProgress = 0,
                    CaseStatus = c.Status,
                    CasePriority = c.Priority,
                    Court = c.Court ?? "",
                    NextHearingDate = c.NextHearingDate,
                    IsUrgent = c.Priority == CasePriority.Urgent,
                    ContractsCount = c.Client.Contracts.Count,
                    AppointmentsCount = c.Client.Appointments.Count(a => a.Status != AppointmentStatus.Cancelled),
                    LastAppointment = c.Client.Appointments
                        .Where(a => a.Status == AppointmentStatus.Confirmed)
                        .OrderByDescending(a => a.Date)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(clients);
        }

        /// <summary>
        /// ✅ إحصائيات موكلين المحامي
        /// </summary>
        [Authorize(Roles = "Lawyer")]
        [HttpGet("my-clients/stats")]
        public async Task<IActionResult> GetMyClientsStats()
        {
            var userId = GetUserId();
            
            var lawyer = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == userId);
            
            if (lawyer == null)
                return NotFound();

            var totalClients = await _context.Cases
                .Where(c => c.LawyerId == lawyer.Id)
                .Select(c => c.ClientId)
                .Distinct()
                .CountAsync();

            var activeCases = await _context.Cases
                .CountAsync(c => c.LawyerId == lawyer.Id && 
                    c.Status != CaseStatus.Completed && 
                    c.Status != CaseStatus.Rejected);

            var urgentCases = await _context.Cases
                .CountAsync(c => c.LawyerId == lawyer.Id && 
                    c.Priority == CasePriority.Urgent);

            var completedCases = await _context.Cases
                .CountAsync(c => c.LawyerId == lawyer.Id && 
                    c.Status == CaseStatus.Completed);

            var upcomingAppointments = await _context.Appointments
                .CountAsync(a => a.LawyerId == lawyer.Id && 
                    a.Date.Date >= DateTime.UtcNow.Date && 
                    a.Status == AppointmentStatus.Confirmed);

            var totalContracts = await _context.Contracts
                .CountAsync(c => c.LawyerId == lawyer.Id);

            return Ok(new
            {
                totalClients,
                activeCases,
                urgentCases,
                completedCases,
                upcomingAppointments,
                totalContracts
            });
        }

        /// <summary>
        /// ✅ إضافة موكل جديد مع قضية (نسخة معدلة بالكامل مع Logging)
        /// </summary>
        [Authorize(Roles = "Lawyer")]
        [HttpPost("add-client")]
        public async Task<IActionResult> AddClient([FromBody] AddClientRequest request)
        {
            try
            {
                // ✅ Log البيانات المستلمة من الـ Frontend
                _logger.LogInformation("=== AddClient Request ===");
                _logger.LogInformation($"ClientName: '{request.ClientName}'");
                _logger.LogInformation($"Phone: '{request.Phone}'");
                _logger.LogInformation($"Email: '{request.Email}'");
                _logger.LogInformation($"CaseTitle: '{request.CaseTitle}'");
                _logger.LogInformation($"CaseDescription: '{request.CaseDescription}'");
                _logger.LogInformation($"Court: '{request.Court}'");
                _logger.LogInformation($"CaseType: '{request.CaseType}'");
                _logger.LogInformation($"IsUrgent: '{request.IsUrgent}'");
                
                // ✅ التحقق من صحة البيانات الأساسية
                if (string.IsNullOrWhiteSpace(request.ClientName))
                {
                    _logger.LogWarning("ClientName is empty");
                    return BadRequest(new { message = "الاسم الكامل مطلوب" });
                }
                
                if (string.IsNullOrWhiteSpace(request.Phone))
                {
                    _logger.LogWarning("Phone is empty");
                    return BadRequest(new { message = "رقم الهاتف مطلوب" });
                }
                
                if (string.IsNullOrWhiteSpace(request.CaseTitle))
                {
                    _logger.LogWarning("CaseTitle is empty");
                    return BadRequest(new { message = "عنوان القضية مطلوب" });
                }

                var userId = GetUserId();
                _logger.LogInformation($"Lawyer UserId: {userId}");
                
                if (userId == Guid.Empty)
                {
                    _logger.LogWarning("User ID is empty - Unauthorized");
                    return Unauthorized(new { message = "يجب تسجيل الدخول" });
                }
                
                var lawyer = await _context.LawyerProfiles
                    .FirstOrDefaultAsync(l => l.UserId == userId);
                
                if (lawyer == null)
                {
                    _logger.LogWarning($"Lawyer not found for UserId: {userId}");
                    return NotFound(new { message = "الملف الشخصي غير موجود" });
                }

                _logger.LogInformation($"Lawyer found: {lawyer.Id}");

                // ✅ تشفير رقم الهاتف للبحث والتخزين
                string encryptedPhone;
                try
                {
                    encryptedPhone = _encryption.Encrypt(request.Phone);
                    _logger.LogInformation($"Phone encrypted successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to encrypt phone");
                    return StatusCode(500, new { message = "خطأ في تشفير رقم الهاتف" });
                }

                // ✅ البحث عن موكل موجود
                var existingClient = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email || u.Phone == encryptedPhone);

                Guid clientId;
                
                if (existingClient != null)
                {
                    clientId = existingClient.UserID;
                    _logger.LogInformation($"Using existing client: {existingClient.Email}");
                }
                else
                {
                    _logger.LogInformation("Creating new client...");
                    
                    // ✅ معالجة الاسم بشكل صحيح
                    var nameParts = request.ClientName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string firstName = nameParts[0];
                    string? lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : null;
                    
                    _logger.LogInformation($"FirstName: '{firstName}', LastName: '{lastName ?? "null"}'");
                    
                    var tempPassword = GenerateRandomPassword();
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
                    var tempNationalId = GenerateTempNationalId();
                    
                    // ✅ إنشاء موكل جديد مع تشفير رقم التليفون
                    var newClient = new User
                    {
                        UserID = Guid.NewGuid(),
                        FirstName = firstName,
                        LastName = lastName,
                        Email = request.Email ?? $"client_{Guid.NewGuid():N}@tempclient.com",
                        PasswordHash = passwordHash,
                        Phone = encryptedPhone,
                        NationalId = tempNationalId,
                        Nationality = "مصري",
                        Role = UserRole.User,
                        Status = AccountStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        JoinDate = DateTime.UtcNow,
                        EmailVerified = false
                    };
                    
                    _context.Users.Add(newClient);
                    
                    var userProfile = new UserProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = newClient.UserID,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = newClient.Email,
                        PhoneNumber = encryptedPhone,
                        NationalId = tempNationalId,
                        CreatedAt = DateTime.UtcNow,
                        LastProfileUpdate = DateTime.UtcNow
                    };
                    
                    _context.UserProfiles.Add(userProfile);
                    await _context.SaveChangesAsync();
                    clientId = newClient.UserID;
                    
                    _logger.LogInformation($"✅ New client created: {newClient.Email}");
                }
                
                // ✅ إنشاء القضية
                var newCase = new Case
                {
                    Id = Guid.NewGuid(),
                    CaseNumber = GenerateCaseNumber(),
                    Title = request.CaseTitle,
                    Description = request.CaseDescription,
                    ClientId = clientId,
                    LawyerId = lawyer.Id,
                    Court = request.Court,
                    Status = CaseStatus.Pending,
                    Priority = request.IsUrgent ? CasePriority.Urgent : CasePriority.Medium,
                    CaseType = request.CaseType,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Cases.Add(newCase);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"✅ Case created: {newCase.CaseNumber}");
                
                return Ok(new
                {
                    message = "تم إضافة الموكل والقضية بنجاح",
                    clientId = clientId,
                    caseId = newCase.Id,
                    caseNumber = newCase.CaseNumber
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error in AddClient");
                return StatusCode(500, new { message = $"خطأ في قاعدة البيانات: {ex.InnerException?.Message ?? ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AddClient");
                return StatusCode(500, new { message = $"خطأ داخلي: {ex.Message}" });
            }
        }

        /// <summary>
        /// ✅ جلب أنواع القضايا
        /// </summary>
        [AllowAnonymous]
        [HttpGet("case-types")]
        public IActionResult GetCaseTypes()
        {
            var caseTypes = new List<string>
            {
                "قانون مدني",
                "قانون تجاري",
                "قانون جنائي",
                "قانون أسرة",
                "قانون عمل",
                "قانون عقاري",
                "قانون إداري",
                "قانون ضريبي"
            };
            return Ok(caseTypes);
        }

        /// <summary>
        /// ✅ جلب أنواع العقود
        /// </summary>
        [AllowAnonymous]
        [HttpGet("contract-types")]
        public IActionResult GetContractTypes()
        {
            var contractTypes = new List<string>
            {
                "عقد إيجار",
                "عقد عمل",
                "عقد بيع",
                "عقد خدمات",
                "عقد شراكة",
                "وكالة قانونية",
                "عقد تسوية",
                "عقد توريد"
            };
            return Ok(contractTypes);
        }

        // ==================== Private Helper Methods ====================

        private string GenerateCaseNumber()
        {
            var count = _context.Cases.Count() + 1;
            return $"CASE-{DateTime.Now.Year}-{count:D6}";
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateTempNationalId()
        {
            var random = new Random();
            return $"TEMP{DateTime.Now.Ticks}{random.Next(1000, 9999)}";
        }
    }

    public class AddReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public Guid? AppointmentId { get; set; }
    }
}