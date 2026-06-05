using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Infrastructure.Services.IService;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class LawyerService : ILawyerService
    {
        private readonly LegalMateDbContext _context;
        private readonly IAIService _aiService;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<LawyerService> _logger;

        public LawyerService(
            LegalMateDbContext context, 
            IAIService aiService,
            IEncryptionService encryption,
            ILogger<LawyerService> logger)
        {
            _context = context;
            _aiService = aiService;
            _encryption = encryption;
            _logger = logger;
        }

        public async Task<List<LawyerSpecialtyResponseDto>> GetSpecialtiesAsync()
        {
            var specialties = await _context.LawyerSpecialties
                .Where(s => s.IsActive)
                .OrderBy(s => s.Id)
                .ToListAsync();

            return specialties.Select(s => new LawyerSpecialtyResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                NameAr = s.NameAr,
                Description = s.Description
            }).ToList();
        }

        public async Task<List<LawyerResponseDto>> SearchLawyersAsync(LawyerSearchDto searchCriteria)
        {
            var query = _context.Users
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Specialties!)
                    .ThenInclude(ls => ls.Specialty)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Reviews)
                .Include(u => u.UserProfile)
                .Where(u => u.Role == UserRole.Lawyer &&
                            u.Status == AccountStatus.Active &&
                            u.LawyerProfile != null);

            if (searchCriteria.SpecializationId.HasValue)
            {
                query = query.Where(u => u.LawyerProfile!.Specialties != null &&
                    u.LawyerProfile.Specialties.Any(ls => ls.SpecialtyId == searchCriteria.SpecializationId.Value));
            }
            else if (!string.IsNullOrEmpty(searchCriteria.Specialization))
            {
                query = query.Where(u => u.LawyerProfile!.Specialties != null &&
                    u.LawyerProfile.Specialties.Any(ls => ls.Specialty != null && 
                        (ls.Specialty.NameAr ?? "").Contains(searchCriteria.Specialization)));
            }

            if (searchCriteria.GovernorateId.HasValue)
            {
                query = query.Where(u => u.LawyerProfile!.GovernorateId == searchCriteria.GovernorateId);
            }
            else if (!string.IsNullOrEmpty(searchCriteria.Location))
            {
                query = query.Where(u => u.LawyerProfile!.City != null &&
                    u.LawyerProfile.City.Contains(searchCriteria.Location));
            }

            if (searchCriteria.MinExperience.HasValue)
            {
                query = query.Where(u => u.LawyerProfile!.YearsOfExperience >= searchCriteria.MinExperience);
            }

            var lawyers = await query.ToListAsync();
            return lawyers.Select(u => MapToDto(u, _encryption)).OrderByDescending(l => l.Rating).ToList();
        }

        public async Task<LawyerResponseDto?> GetLawyerByIdAsync(Guid lawyerId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Specialties!)
                    .ThenInclude(ls => ls.Specialty)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Certificates)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Reviews)
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && 
                                          u.Role == UserRole.Lawyer &&
                                          u.LawyerProfile != null &&
                                          u.Status == AccountStatus.Active);

            return user?.LawyerProfile == null ? null : MapToDto(user, _encryption);
        }

        public async Task<List<AvailabilityDto>> GetLawyerAvailabilityAsync(Guid lawyerId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && 
                                          u.Role == UserRole.Lawyer &&
                                          u.Status == AccountStatus.Active);
            
            if (user?.LawyerProfile == null) return new List<AvailabilityDto>();

            return await _context.LawyerAvailabilities
                .Where(a => a.LawyerId == user.LawyerProfile.Id)
                .OrderBy(a => a.Day).ThenBy(a => a.StartTime)
                .Select(a => new AvailabilityDto
                {
                    Id = a.Id,
                    Day = a.Day,
                    DayName = GetDayNameAr(a.Day),
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    IsAvailable = a.IsAvailable
                }).ToListAsync();
        }

        public async Task<bool> UpdateAvailabilityAsync(Guid lawyerId, List<CreateLawyerAvailabilityDto> availabilities)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && 
                                          u.Role == UserRole.Lawyer &&
                                          u.Status == AccountStatus.Active);
            
            if (user?.LawyerProfile == null) return false;

            var existing = await _context.LawyerAvailabilities
                .Where(a => a.LawyerId == user.LawyerProfile.Id).ToListAsync();
            _context.LawyerAvailabilities.RemoveRange(existing);

            foreach (var avail in availabilities)
            {
                _context.LawyerAvailabilities.Add(new LawyerAvailability
                {
                    Id = Guid.NewGuid(),
                    LawyerId = user.LawyerProfile.Id,
                    Day = avail.Day,
                    DayName = GetDayNameAr(avail.Day),
                    StartTime = avail.StartTime,
                    EndTime = avail.EndTime,
                    IsAvailable = avail.IsAvailable
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ReviewDto>> GetLawyerReviewsAsync(Guid lawyerId)
        {
            _logger.LogInformation($"Getting reviews for lawyer: {lawyerId}");

            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.Id == lawyerId);
            
            if (lawyerProfile == null)
            {
                _logger.LogWarning($"Lawyer not found: {lawyerId}");
                return new List<ReviewDto>();
            }

            var reviews = await _context.LawyerReviews
                .Include(r => r.User)
                .Where(r => r.LawyerId == lawyerProfile.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = r.User.FullName ?? "مستخدم",
                    UserImage = r.User.ProfilePicture,
                    Rating = r.Rating,
                    Comment = r.Comment ?? "",
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            _logger.LogInformation($"Found {reviews.Count} reviews for lawyer {lawyerId}");
            return reviews;
        }

        public async Task<bool> AddReviewAsync(Guid userId, Guid lawyerId, int rating, string? comment, Guid? appointmentId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId);
            
            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.Id == lawyerId);
                
            if (lawyerProfile == null) return false;
            
            var lawyerUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == lawyerProfile.UserId && u.Status == AccountStatus.Active);
            
            if (lawyerUser == null) return false;

            var existingReview = await _context.LawyerReviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.LawyerId == lawyerId);
            if (existingReview != null) return false;

            _context.LawyerReviews.Add(new LawyerReview
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LawyerId = lawyerId,
                Rating = rating,
                Comment = comment,
                AppointmentId = appointmentId,
                CreatedAt = DateTime.UtcNow,
                IsVerified = true
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<LawyerResponseDto>> GetLawyersBySpecializationAsync(string specialization, int limit = 5)
        {
            var users = await _context.Users
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Specialties!)
                    .ThenInclude(ls => ls.Specialty)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Reviews)
                .Include(u => u.UserProfile)
                .Where(u => u.Role == UserRole.Lawyer && 
                            u.Status == AccountStatus.Active &&
                            u.LawyerProfile!.Specialties != null &&
                            u.LawyerProfile.Specialties.Any(ls => ls.Specialty != null && ls.Specialty.NameAr.Contains(specialization)))
                .Take(limit)
                .ToListAsync();

            return users.OrderByDescending(u => 
                u.LawyerProfile!.Reviews?.Any() == true 
                    ? u.LawyerProfile.Reviews.Average(r => r.Rating) 
                    : 0)
                .Select(u => MapToDto(u, _encryption))
                .ToList();
        }

        private static string GetDayNameAr(DayOfWeek day) => day switch
        {
            DayOfWeek.Sunday => "الأحد",
            DayOfWeek.Monday => "الاثنين",
            DayOfWeek.Tuesday => "الثلاثاء",
            DayOfWeek.Wednesday => "الأربعاء",
            DayOfWeek.Thursday => "الخميس",
            DayOfWeek.Friday => "الجمعة",
            DayOfWeek.Saturday => "السبت",
            _ => day.ToString()
        };

        private static string? Decrypt(string? encrypted, IEncryptionService encryption)
        {
            if (string.IsNullOrEmpty(encrypted)) return null;
            try { return encryption.Decrypt(encrypted); }
            catch { return encrypted; }
        }

        private static LawyerResponseDto MapToDto(User user, IEncryptionService encryption)
        {
            var lawyer = user.LawyerProfile!;
            var avgRating = lawyer.Reviews?.Any() == true ? lawyer.Reviews.Average(r => r.Rating) : 0;

            string? decryptedPhone = Decrypt(user.Phone, encryption);
            string? decryptedNationalId = Decrypt(user.NationalId, encryption);
            string? decryptedLicense = Decrypt(lawyer.LicenseNumber, encryption);
            string? decryptedDateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd");
            string? decryptedCreatedAt = user.CreatedAt.ToString("yyyy-MM-dd");
            string? decryptedVerifiedAt = lawyer.VerifiedAt?.ToString("yyyy-MM-dd");
            string? decryptedNationality = user.Nationality;

            // ✅ تحويل المهارات من نص إلى قائمة
            var skillsList = string.IsNullOrEmpty(lawyer.Skills)
                ? new List<string>()
                : lawyer.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

            // ✅ حساب عدد القضايا النشطة
            var activeCasesCount = lawyer.Specialties?.Count ?? 0;

            return new LawyerResponseDto
            {
                Id = lawyer.Id,
                UserId = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = decryptedPhone ?? "",
                AlternativePhone = user.UserProfile != null ? Decrypt(user.UserProfile.AlternativePhone, encryption) : null,
                NationalId = decryptedNationalId ?? "",
                Nationality = decryptedNationality ?? "",
                ProfilePicture = user.ProfilePicture ?? "",
                LicenseNumber = decryptedLicense ?? "",
                BarAssociation = lawyer.BarAssociation ?? "",
                YearsOfExperience = lawyer.YearsOfExperience ?? 0,
                Status = user.Status,
                VerifiedAt = lawyer.VerifiedAt,
                VerifiedAtFormatted = decryptedVerifiedAt,
                RejectionReason = lawyer.RejectionReason,
                SuspensionReason = lawyer.SuspensionReason ?? user.SuspensionReason,
                SuspendedAt = lawyer.SuspendedAt ?? user.SuspendedAt,
                ActivatedAt = lawyer.ActivatedAt ?? user.ActivatedAt,
                Rating = Math.Round(avgRating, 1),
                TotalReviews = lawyer.Reviews?.Count ?? 0,
                GovernorateId = lawyer.GovernorateId,
                GovernorateName = lawyer.Governorate,
                City = lawyer.City ?? "",
                OfficeAddress = lawyer.OfficeAddress,
                DateOfBirth = decryptedDateOfBirth,
                CreatedAt = decryptedCreatedAt,
                
                // ✅ المهارات والإحصائيات الجديدة
                Skills = skillsList,
                ApprovedLitigationsCount = lawyer.ApprovedLitigationsCount,
                ClientsCount = lawyer.ClientsCount,
                ActiveCasesCount = activeCasesCount,
                
                Specialties = lawyer.Specialties?.Select(s => new LawyerProfileSpecialtyDto
                {
                    Id = s.SpecialtyId,
                    Name = s.Specialty?.Name ?? "",
                    NameAr = s.Specialty?.NameAr ?? "",
                    IsPrimary = s.IsPrimary,
                    YearsOfExperience = s.YearsOfExperience
                }).ToList() ?? new(),
                Certificates = lawyer.Certificates?.Select(c => new LegalMateAI.DTOs.ReadDTO.CertificateDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    IssuingOrganization = c.IssuingOrganization,
                    Year = c.Year,
                    FileUrl = c.FileUrl
                }).ToList() ?? new()
            };
        }
    }
}