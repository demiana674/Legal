// LegalMateAI.BLL/Services/Service/LawyerProfileService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Infrastructure.Services.IService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using LegalMateAI.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class LawyerProfileService : ILawyerProfileService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<LawyerProfileService> _logger;

        public LawyerProfileService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            IEncryptionService encryption,
            ILogger<LawyerProfileService> logger)
        {
            _context = context;
            _env = env;
            _encryption = encryption;
            _logger = logger;
        }

        public async Task<LawyerProfileDto?> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(s => s.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Governorate)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user == null || user.LawyerProfile == null) return null;

            var lawyerProfile = user.LawyerProfile;

            // فك تشفير البيانات
            string? decryptedLicense = Decrypt(lawyerProfile.LicenseNumber);
            string? decryptedPhone = Decrypt(lawyerProfile.PhoneNumber ?? user.Phone);
            string? decryptedAltPhone = Decrypt(lawyerProfile.AlternativePhone);
            string? decryptedNationalId = Decrypt(user.NationalId);

            // إحصائيات
            var activeCases = await _context.Cases
                .CountAsync(c => c.LawyerId == lawyerProfile.Id && c.Status != CaseStatus.Completed);
            
            var upcomingHearings = await _context.Cases
                .CountAsync(c => c.LawyerId == lawyerProfile.Id && 
                    c.NextHearingDate.HasValue && 
                    c.NextHearingDate.Value >= DateTime.UtcNow.Date &&
                    c.NextHearingDate.Value <= DateTime.UtcNow.Date.AddDays(7));

            var totalClients = await _context.Cases
                .Where(c => c.LawyerId == lawyerProfile.Id)
                .Select(c => c.ClientId)
                .Distinct()
                .CountAsync();

            return new LawyerProfileDto
            {
                Id = lawyerProfile.Id,
                UserId = user.UserID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = decryptedPhone,
                NationalId = decryptedNationalId,
                ProfilePicture = user.ProfilePicture,
                LicenseNumber = decryptedLicense ?? "",
                BarAssociation = lawyerProfile.BarAssociation ?? "",
                YearsOfExperience = lawyerProfile.YearsOfExperience ?? 0,
                LicenseIssueDate = lawyerProfile.LicenseIssueDate,
                PracticeDegree = lawyerProfile.PracticeDegree,
                GovernorateId = lawyerProfile.GovernorateId,
                GovernorateName = lawyerProfile.Governorate?.Name,
                City = lawyerProfile.City?.Name,
                OfficeAddress = lawyerProfile.OfficeAddress,
                AlternativePhone = decryptedAltPhone,
                VerificationStatus = lawyerProfile.VerificationStatus.ToString(),
                VerifiedAt = lawyerProfile.VerifiedAt,
                IsActive = user.IsActive,
                RejectionReason = lawyerProfile.RejectionReason,
                ActiveCases = activeCases,
                UpcomingHearings = upcomingHearings,
                TotalClients = totalClients,
                CreatedAt = user.CreatedAt,
                Specializations = lawyerProfile.Specialties?.Select(s => new SpecializationDto
                {
                    Id = s.SpecialtyId,
                    Name = s.Specialty?.NameAr ?? "",
                    IsPrimary = s.IsPrimary,
                    CasesCount = s.YearsOfExperience
                }).ToList() ?? new List<SpecializationDto>()
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateLawyerProfileDto request)
        {
            _logger.LogInformation($"UpdateProfileAsync called for lawyer: {userId}");

            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user == null || user.LawyerProfile == null) return false;

            var lawyerProfile = user.LawyerProfile;
            bool hasChanges = false;

            if (!string.IsNullOrEmpty(request.Phone))
            {
                user.Phone = _encryption.Encrypt(request.Phone);
                lawyerProfile.PhoneNumber = _encryption.Encrypt(request.Phone);
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.AlternativePhone))
            {
                lawyerProfile.AlternativePhone = _encryption.Encrypt(request.AlternativePhone);
                hasChanges = true;
            }

            if (request.SpecialtyIds != null && request.SpecialtyIds.Any())
            {
                if (lawyerProfile.Specialties.Any())
                    _context.LawyerProfileSpecialties.RemoveRange(lawyerProfile.Specialties);

                var isFirst = true;
                foreach (var specId in request.SpecialtyIds)
                {
                    var specialty = await _context.LawyerSpecialties.FindAsync(specId);
                    if (specialty != null)
                    {
                        _context.LawyerProfileSpecialties.Add(new LawyerProfileSpecialty
                        {
                            Id = Guid.NewGuid(),
                            LawyerId = lawyerProfile.Id,
                            SpecialtyId = specId,
                            IsPrimary = isFirst,
                            YearsOfExperience = 0
                        });
                        isFirst = false;
                    }
                }
                hasChanges = true;
            }

            if (request.GovernorateId.HasValue)
            {
                lawyerProfile.GovernorateId = request.GovernorateId;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.City))
            {
                lawyerProfile.CityId = int.TryParse(request.City, out var cityId) ? cityId : null;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.OfficeAddress))
            {
                lawyerProfile.OfficeAddress = request.OfficeAddress;
                hasChanges = true;
            }

            if (hasChanges)
            {
                lawyerProfile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            return true;
        }

        public async Task<string?> UploadProfilePictureAsync(Guid userId, IFormFile file)
        {
            if (file == null || file.Length == 0) return null;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension) || file.Length > 2 * 1024 * 1024) return null;

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != UserRole.Lawyer) return null;

            var uploadsFolder = Path.Combine(_env.WebRootPath, "profiles", "lawyers");
            Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldFile = Path.Combine(_env.WebRootPath, user.ProfilePicture.TrimStart('/'));
                if (File.Exists(oldFile)) File.Delete(oldFile);
            }

            var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }

            var pictureUrl = $"/profiles/lawyers/{fileName}";
            user.ProfilePicture = pictureUrl;
            await _context.SaveChangesAsync();
            return pictureUrl;
        }

        public async Task<bool> RemoveProfilePictureAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.ProfilePicture)) return false;

            var filePath = Path.Combine(_env.WebRootPath, user.ProfilePicture.TrimStart('/'));
            if (File.Exists(filePath)) File.Delete(filePath);

            user.ProfilePicture = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LawyerProfileDto?> GetDashboardAsync(Guid userId)
        {
            return await GetProfileAsync(userId);
        }

        private string? Decrypt(string? encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return null;
            try { return _encryption.Decrypt(encrypted); }
            catch { return encrypted; }
        }
    }
}