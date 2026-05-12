// LegalMateAI.BLL/Services/Service/LawyerService.cs
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
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(ls => ls.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .Where(u => u.Role == UserRole.Lawyer &&
                            u.IsActive == true &&
                            u.LawyerProfile != null &&
                            u.LawyerProfile.VerificationStatus == LawyerVerificationStatus.Active);

            if (searchCriteria.SpecializationId.HasValue)
            {
                query = query.Where(u => u.LawyerProfile!.Specialties
                    .Any(ls => ls.SpecialtyId == searchCriteria.SpecializationId));
            }
            else if (!string.IsNullOrEmpty(searchCriteria.Specialization))
            {
                query = query.Where(u => u.LawyerProfile!.Specialties
                    .Any(ls => ls.Specialty.NameAr.Contains(searchCriteria.Specialization)));
            }

            if (searchCriteria.GovernorateId.HasValue)
            {
                query = query.Where(u => u.LawyerProfile!.GovernorateId == searchCriteria.GovernorateId);
            }
            else if (!string.IsNullOrEmpty(searchCriteria.Location))
            {
                query = query.Where(u => u.LawyerProfile!.City != null &&
                    u.LawyerProfile.City.Name.Contains(searchCriteria.Location));
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
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(ls => ls.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Certificates)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && 
                                          u.Role == UserRole.Lawyer &&
                                          u.LawyerProfile != null &&
                                          u.LawyerProfile.VerificationStatus == LawyerVerificationStatus.Active);

            return user?.LawyerProfile == null ? null : MapToDto(user, _encryption);
        }

        public async Task<List<AvailabilityDto>> GetLawyerAvailabilityAsync(Guid lawyerId)
        {
            var lawyer = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == lawyerId && 
                                          l.VerificationStatus == LawyerVerificationStatus.Active);
            
            if (lawyer == null) return new List<AvailabilityDto>();

            return await _context.LawyerAvailabilities
                .Where(a => a.LawyerId == lawyer.Id)
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
            var lawyer = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == lawyerId && 
                                          l.VerificationStatus == LawyerVerificationStatus.Active);
            
            if (lawyer == null) return false;

            var existing = await _context.LawyerAvailabilities
                .Where(a => a.LawyerId == lawyer.Id).ToListAsync();
            _context.LawyerAvailabilities.RemoveRange(existing);

            foreach (var avail in availabilities)
            {
                _context.LawyerAvailabilities.Add(new LawyerAvailability
                {
                    Id = Guid.NewGuid(),
                    LawyerId = lawyer.Id,
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
            return await _context.LawyerReviews
                .Include(r => r.User)
                .Where(r => r.LawyerId == lawyerId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = r.User.FullName ?? "",
                    UserImage = r.User.ProfilePicture,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToListAsync();
        }

        public async Task<bool> AddReviewAsync(Guid userId, Guid lawyerId, int rating, string? comment, Guid? appointmentId)
        {
            var lawyer = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.Id == lawyerId && 
                                          l.VerificationStatus == LawyerVerificationStatus.Active);
            if (lawyer == null) return false;

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
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<LawyerResponseDto>> GetLawyersBySpecializationAsync(string specialization, int limit = 5)
        {
            // ✅ نجيب البيانات الأول من الداتابيز
            var users = await _context.Users
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(ls => ls.Specialty)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Reviews)
                .Where(u => u.Role == UserRole.Lawyer && 
                            u.IsActive &&
                            u.LawyerProfile!.VerificationStatus == LawyerVerificationStatus.Active &&
                            u.LawyerProfile.Specialties.Any(ls => ls.Specialty.NameAr.Contains(specialization)))
                .Take(limit)
                .ToListAsync();

            // ✅ وبعدين نحول لـ DTO في الذاكرة (مش في الداتابيز)
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

            return new LawyerResponseDto
            {
                Id = lawyer.Id,
                UserId = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = Decrypt(user.Phone, encryption) ?? "",
                ProfilePicture = user.ProfilePicture ?? "",
                LicenseNumber = Decrypt(lawyer.LicenseNumber, encryption) ?? "",
                BarAssociation = lawyer.BarAssociation ?? "",
                YearsOfExperience = lawyer.YearsOfExperience ?? 0,
                VerificationStatus = lawyer.VerificationStatus.ToString(),
                IsActive = user.IsActive,
                VerifiedAt = lawyer.VerifiedAt,
                RejectionReason = lawyer.RejectionReason,
                Rating = Math.Round(avgRating, 1),
                TotalReviews = lawyer.Reviews?.Count ?? 0,
                GovernorateId = lawyer.GovernorateId,
                GovernorateName = lawyer.Governorate?.Name,
                City = lawyer.City?.Name,
                OfficeAddress = lawyer.OfficeAddress,
                Specialties = lawyer.Specialties?.Select(s => new LawyerProfileSpecialtyDto
                {
                    Id = s.SpecialtyId,
                    Name = s.Specialty?.Name ?? "",
                    NameAr = s.Specialty?.NameAr ?? "",
                    IsPrimary = s.IsPrimary,
                    YearsOfExperience = s.YearsOfExperience
                }).ToList() ?? new(),
                Certificates = lawyer.Certificates?.Select(c => new CertificateDto
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