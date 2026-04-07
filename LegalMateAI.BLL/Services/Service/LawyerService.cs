// LegalMateAI.BLL/Services/Service/LawyerService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.BLL.Services.IService;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class LawyerService : ILawyerService
    {
        private readonly LegalMateDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<LawyerService> _logger;

        public LawyerService(
            LegalMateDbContext context, 
            IAIService aiService,
            ILogger<LawyerService> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        // ✅ جلب تخصصات المحامي
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
                    u.LawyerProfile.City.Contains(searchCriteria.Location));
            }

            if (searchCriteria.MinExperience.HasValue)
            {
                query = query.Where(u => u.LawyerProfile!.YearsOfExperience >= searchCriteria.MinExperience);
            }

            if (searchCriteria.MinRating.HasValue)
            {
                query = query.Where(u => u.LawyerProfile!.Reviews.Average(r => r.Rating) >= searchCriteria.MinRating);
            }

            var lawyers = await query.ToListAsync();
            var result = lawyers.Select(u => MapToDto(u)).ToList();

            if (result.Any())
            {
                result = result.OrderByDescending(l => l.Rating).ToList();
            }

            return result;
        }

        public async Task<LawyerResponseDto?> GetLawyerByIdAsync(Guid lawyerId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(ls => ls.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Certificates)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null) return null;

            return MapToDto(user);
        }

        public async Task<List<AvailabilityDto>> GetLawyerAvailabilityAsync(Guid lawyerId)
        {
            var lawyer = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == lawyerId);

            if (lawyer == null)
            {
                _logger.LogWarning($"Lawyer profile not found for UserId: {lawyerId}");
                return new List<AvailabilityDto>();
            }

            var availabilities = await _context.LawyerAvailabilities
                .Where(a => a.LawyerId == lawyer.Id)
                .OrderBy(a => a.Day)
                .ThenBy(a => a.StartTime)
                .ToListAsync();

            return availabilities.Select(a => new AvailabilityDto
            {
                Id = a.Id,
                Day = a.Day,
                DayName = GetDayNameAr(a.Day),
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                IsAvailable = a.IsAvailable
            }).ToList();
        }

        public async Task<bool> UpdateAvailabilityAsync(Guid lawyerId, List<CreateLawyerAvailabilityDto> availabilities)
        {
            _logger.LogInformation($"UpdateAvailabilityAsync called for UserId: {lawyerId}");

            var lawyer = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == lawyerId);

            if (lawyer == null)
            {
                _logger.LogWarning($"Lawyer profile not found for UserId: {lawyerId}");
                return false;
            }

            var existing = await _context.LawyerAvailabilities
                .Where(a => a.LawyerId == lawyer.Id)
                .ToListAsync();

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
            var reviews = await _context.LawyerReviews
                .Include(r => r.User)
                .Where(r => r.LawyerId == lawyerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                UserName = r.User?.FullName ?? "",
                UserImage = r.User?.ProfilePicture,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<bool> AddReviewAsync(Guid userId, Guid lawyerId, int rating, string? comment, Guid? appointmentId)
        {
            if (appointmentId.HasValue)
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == appointmentId.Value &&
                        a.UserID == userId &&
                        a.LawyerId == lawyerId &&
                        a.Status == AppointmentStatus.Completed);

                if (appointment == null) return false;
            }

            var existingReview = await _context.LawyerReviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.LawyerId == lawyerId);

            if (existingReview != null) return false;

            var review = new LawyerReview
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LawyerId = lawyerId,
                Rating = rating,
                Comment = comment,
                AppointmentId = appointmentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.LawyerReviews.Add(review);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<LawyerResponseDto>> GetLawyersBySpecializationAsync(string specialization, int limit = 5)
        {
            var lawyers = await _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(ls => ls.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .Where(u => u.Role == UserRole.Lawyer &&
                            u.IsActive == true &&
                            u.LawyerProfile != null &&
                            u.LawyerProfile.VerificationStatus == LawyerVerificationStatus.Active &&
                            u.LawyerProfile.Specialties.Any(ls => ls.Specialty.NameAr.Contains(specialization)))
                .OrderByDescending(u => u.LawyerProfile!.Reviews.Average(r => r.Rating))
                .Take(limit)
                .ToListAsync();

            return lawyers.Select(u => MapToDto(u)).ToList();
        }

        #region Private Methods

        private string GetDayNameAr(DayOfWeek day)
        {
            return day switch
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
        }

        private LawyerResponseDto MapToDto(User user)
        {
            var lawyer = user.LawyerProfile!;
            var averageRating = lawyer.Reviews?.Any() == true ? lawyer.Reviews.Average(r => r.Rating) : 0;

            return new LawyerResponseDto
            {
                Id = lawyer.Id,
                UserId = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.Phone ?? "",
                ProfilePicture = user.ProfilePicture ?? "",
                LicenseNumber = lawyer.LicenseNumber ?? "",
                BarAssociation = lawyer.BarAssociation ?? "",
                YearsOfExperience = lawyer.YearsOfExperience ?? 0,
                VerificationStatus = lawyer.VerificationStatus.ToString(),
                IsActive = user.IsActive,
                VerifiedAt = lawyer.VerifiedAt,
                RejectionReason = lawyer.RejectionReason,
                Rating = Math.Round(averageRating, 1),
                TotalReviews = lawyer.Reviews?.Count ?? 0,
                GovernorateId = lawyer.GovernorateId,
                GovernorateName = lawyer.Governorate?.Name,
                City = lawyer.City,
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

        #endregion
    }
}