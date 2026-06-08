using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Infrastructure.Services.IService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class LawyerProfileService : ILawyerProfileService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<LawyerProfileService> _logger;

        public LawyerProfileService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            IEncryptionService encryption,
            IHttpContextAccessor httpContextAccessor,
            ILogger<LawyerProfileService> logger)
        {
            _context = context;
            _env = env;
            _encryption = encryption;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private string? DecryptSafely(string? encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return null;
            try { return _encryption.Decrypt(encrypted); }
            catch (Exception ex)
            {
                _logger.LogWarning($"Decryption failed: {ex.Message}");
                return encrypted;
            }
        }

        public async Task<LawyerProfileDto?> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Specializations!)
                    .ThenInclude(s => s.Specialization)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Certificates)
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Reviews)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user == null || user.LawyerProfile == null) return null;

            var lawyerProfile = user.LawyerProfile;

            // ✅ جلب التخصصات مع IsPrimary و YearsOfExperience
            var specializationsList = lawyerProfile.Specializations?
                .Select(s => new LawyerProfileSpecialtyDto
                {
                    Id = s.SpecializationId,
                    Name = s.Specialization?.Name ?? "",
                    NameAr = s.Specialization?.NameAr ?? "",
                    IsPrimary = s.IsPrimary,
                    YearsOfExperience = s.YearsOfExperience
                }).ToList() ?? new List<LawyerProfileSpecialtyDto>();

            string? decryptedLicense = DecryptSafely(lawyerProfile.LicenseNumber);
            string? decryptedPhone = DecryptSafely(user.Phone);
            string? decryptedAltPhone = DecryptSafely(lawyerProfile.AlternativePhone);
            string? decryptedNationalId = DecryptSafely(user.NationalId);
            string? decryptedDateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd");

            var activeCases = await _context.Cases
                .CountAsync(c => c.LawyerId == lawyerProfile.Id && 
                    c.Status != CaseStatus.Completed && 
                    c.Status != CaseStatus.Rejected);

            var upcomingHearings = await _context.Cases
                .CountAsync(c => c.LawyerId == lawyerProfile.Id &&
                    c.NextHearingDate.HasValue &&
                    c.NextHearingDate.Value >= DateTime.UtcNow.Date &&
                    c.NextHearingDate.Value <= DateTime.UtcNow.Date.AddDays(7));

            var actualClientsCount = await _context.Cases
                .Where(c => c.LawyerId == lawyerProfile.Id)
                .Select(c => c.ClientId)
                .Distinct()
                .CountAsync();

            var avgRating = lawyerProfile.Reviews?.Any() == true 
                ? lawyerProfile.Reviews.Average(r => r.Rating) 
                : 0;

            var totalReviews = lawyerProfile.Reviews?.Count ?? 0;

            var skillsList = string.IsNullOrEmpty(lawyerProfile.Skills)
                ? new List<string>()
                : lawyerProfile.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

            var certificatesList = lawyerProfile.Certificates?
                .Select(c => new CertificateDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    IssuingOrganization = c.IssuingOrganization,
                    Year = c.Year,
                    FileUrl = c.FileUrl
                }).ToList() ?? new List<CertificateDto>();

            return new LawyerProfileDto
            {
                Id = lawyerProfile.Id,
                UserId = user.UserID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = decryptedPhone ?? user.Phone,
                AlternativePhone = decryptedAltPhone,
                NationalId = decryptedNationalId ?? user.NationalId,
                Nationality = user.Nationality ?? "",
                ProfilePicture = user.ProfilePicture,
                DateOfBirth = decryptedDateOfBirth ?? "",
                LicenseNumber = decryptedLicense ?? "",
                BarAssociation = lawyerProfile.BarAssociation ?? "",
                YearsOfExperience = lawyerProfile.YearsOfExperience ?? 0,
                LicenseIssueDate = lawyerProfile.LicenseIssueDate,
                PracticeDegree = lawyerProfile.PracticeDegree,
                GovernorateId = lawyerProfile.GovernorateId,
                GovernorateName = lawyerProfile.Governorate,
                City = lawyerProfile.City,
                OfficeAddress = lawyerProfile.OfficeAddress,
                Status = user.Status,
                VerifiedAt = lawyerProfile.VerifiedAt,
                RejectionReason = lawyerProfile.RejectionReason,
                SuspensionReason = user.SuspensionReason,
                SuspendedAt = user.SuspendedAt,
                ActivatedAt = user.ActivatedAt,
                ActiveCases = activeCases,
                UpcomingHearings = upcomingHearings,
                TotalClients = actualClientsCount,
                CreatedAt = user.CreatedAt,
                Skills = skillsList,
                ApprovedLitigationsCount = lawyerProfile.ApprovedLitigationsCount,
                ClientsCount = actualClientsCount,
                TotalReviews = totalReviews,
                AverageRating = avgRating,
                Specializations = specializationsList,
                Certificates = certificatesList
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateLawyerProfileDto request)
        {
            _logger.LogInformation($"🔵 UpdateProfileAsync called for lawyer: {userId}");

            var user = await _context.Users
                .Include(u => u.LawyerProfile!)
                    .ThenInclude(lp => lp!.Specializations)
                .AsTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user == null || user.LawyerProfile == null)
            {
                _logger.LogWarning($"❌ User not found for userId: {userId}");
                return false;
            }

            var lawyerProfile = user.LawyerProfile;
            bool hasChanges = false;

            // Basic Info
            if (!string.IsNullOrEmpty(request.FirstName))
            {
                user.FirstName = request.FirstName;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.LastName))
            {
                user.LastName = request.LastName;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.Email))
            {
                user.Email = request.Email;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.Phone))
            {
                try
                {
                    user.Phone = _encryption.Encrypt(request.Phone);
                    lawyerProfile.PhoneNumber = _encryption.Encrypt(request.Phone);
                }
                catch
                {
                    user.Phone = request.Phone;
                    lawyerProfile.PhoneNumber = request.Phone;
                }
                hasChanges = true;
            }
            
            // معالجة الهاتف البديل - يخزن في LawyerProfile
            if (request.AlternativePhone != null)
            {
                try
                {
                    lawyerProfile.AlternativePhone = string.IsNullOrEmpty(request.AlternativePhone)
                        ? null
                        : _encryption.Encrypt(request.AlternativePhone);
                }
                catch
                {
                    lawyerProfile.AlternativePhone = request.AlternativePhone;
                }
                hasChanges = true;
            }
            
            if (!string.IsNullOrEmpty(request.Nationality))
            {
                user.Nationality = request.Nationality;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.DateOfBirth))
            {
                if (DateTime.TryParse(request.DateOfBirth, out var dob))
                {
                    user.DateOfBirth = dob;
                    hasChanges = true;
                }
            }
            if (!string.IsNullOrEmpty(request.NationalId))
            {
                try
                {
                    user.NationalId = _encryption.Encrypt(request.NationalId);
                }
                catch
                {
                    user.NationalId = request.NationalId;
                }
                hasChanges = true;
            }

            // Professional Info
            if (request.YearsOfExperience.HasValue)
            {
                lawyerProfile.YearsOfExperience = request.YearsOfExperience.Value;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.BarAssociation))
            {
                lawyerProfile.BarAssociation = request.BarAssociation;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.LicenseNumber))
            {
                try
                {
                    lawyerProfile.LicenseNumber = _encryption.Encrypt(request.LicenseNumber);
                }
                catch
                {
                    lawyerProfile.LicenseNumber = request.LicenseNumber;
                }
                hasChanges = true;
            }
            if (request.LicenseIssueDate.HasValue)
            {
                lawyerProfile.LicenseIssueDate = request.LicenseIssueDate;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.PracticeDegree))
            {
                lawyerProfile.PracticeDegree = request.PracticeDegree;
                hasChanges = true;
            }

            // Location
            if (request.GovernorateId.HasValue)
            {
                lawyerProfile.GovernorateId = request.GovernorateId.Value;
                hasChanges = true;
            }
            if (request.CityId.HasValue)
            {
                lawyerProfile.CityId = request.CityId.Value;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.Governorate))
            {
                lawyerProfile.Governorate = request.Governorate;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.City))
            {
                lawyerProfile.City = request.City;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(request.OfficeAddress))
            {
                lawyerProfile.OfficeAddress = request.OfficeAddress;
                hasChanges = true;
            }

            // Skills
            if (request.Skills != null && request.Skills.Any())
            {
                lawyerProfile.Skills = string.Join(",", request.Skills);
                hasChanges = true;
            }
            else if (!string.IsNullOrEmpty(request.SkillsText))
            {
                lawyerProfile.Skills = request.SkillsText;
                hasChanges = true;
            }

            // Approved Litigations Count
            if (request.ApprovedLitigationsCount.HasValue)
            {
                lawyerProfile.ApprovedLitigationsCount = request.ApprovedLitigationsCount.Value;
                hasChanges = true;
            }

            // ✅ Specializations - تحديث التخصصات مع IsPrimary و YearsOfExperience
            if (request.Specializations != null && request.Specializations.Any())
            {
                var oldSpecializations = await _context.LawyerSpecializations
                    .Where(ls => ls.LawyerId == lawyerProfile.Id)
                    .ToListAsync();
                
                if (oldSpecializations.Any())
                {
                    _context.LawyerSpecializations.RemoveRange(oldSpecializations);
                }

                foreach (var spec in request.Specializations)
                {
                    if (string.IsNullOrWhiteSpace(spec.Name)) continue;

                    var specialty = await _context.LawyerSpecialties
                        .FirstOrDefaultAsync(s => s.Name == spec.Name || s.NameAr == spec.Name);

                    if (specialty == null)
                    {
                        specialty = new LawyerSpecialty
                        {
                            Name = spec.Name,
                            NameAr = spec.NameAr,
                            IsActive = true
                        };
                        await _context.LawyerSpecialties.AddAsync(specialty);
                        await _context.SaveChangesAsync();
                    }

                    var lawyerSpecialization = new LawyerSpecialization
                    {
                        Id = Guid.NewGuid(),
                        LawyerId = lawyerProfile.Id,
                        SpecializationId = specialty.Id,
                        IsPrimary = spec.IsPrimary,
                        CasesCount = 0,
                        YearsOfExperience = spec.YearsOfExperience
                    };
                    _context.LawyerSpecializations.Add(lawyerSpecialization);
                }
                hasChanges = true;
            }
            else if (request.SpecialtyIds != null && request.SpecialtyIds.Any())
            {
                // التعامل مع SpecialtyIds القديم
                var oldSpecializations = await _context.LawyerSpecializations
                    .Where(ls => ls.LawyerId == lawyerProfile.Id)
                    .ToListAsync();
                if (oldSpecializations.Any())
                {
                    _context.LawyerSpecializations.RemoveRange(oldSpecializations);
                }

                foreach (var specialtyId in request.SpecialtyIds)
                {
                    var specialty = await _context.LawyerSpecialties
                        .FirstOrDefaultAsync(s => s.Id == specialtyId);

                    if (specialty != null)
                    {
                        var lawyerSpecialization = new LawyerSpecialization
                        {
                            Id = Guid.NewGuid(),
                            LawyerId = lawyerProfile.Id,
                            SpecializationId = specialty.Id,
                            IsPrimary = false,
                            CasesCount = 0,
                            YearsOfExperience = 0
                        };
                        _context.LawyerSpecializations.Add(lawyerSpecialization);
                    }
                }
                hasChanges = true;
            }

            // Certificates
            if (request.Certificates != null)
            {
                var oldCertificates = await _context.Certificates
                    .Where(c => c.LawyerId == lawyerProfile.Id)
                    .ToListAsync();
                if (oldCertificates.Any())
                {
                    _context.Certificates.RemoveRange(oldCertificates);
                }

                foreach (var cert in request.Certificates)
                {
                    if (string.IsNullOrWhiteSpace(cert.Name)) continue;

                    var newCert = new Certificate
                    {
                        Id = Guid.NewGuid(),
                        LawyerId = lawyerProfile.Id,
                        Name = cert.Name,
                        IssuingOrganization = cert.IssuingOrganization,
                        Year = cert.Year,
                        FileUrl = cert.FileUrl
                    };
                    _context.Certificates.Add(newCert);
                }
                hasChanges = true;
            }

            if (hasChanges)
            {
                lawyerProfile.UpdatedAt = DateTime.UtcNow;
                
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"✅ Lawyer profile updated successfully for user: {userId}");
                    return true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, $"❌ Concurrency error while updating lawyer {userId}");
                    await ex.Entries.Single().ReloadAsync();
                    _context.Entry(user).CurrentValues.SetValues(user);
                    _context.Entry(lawyerProfile).CurrentValues.SetValues(lawyerProfile);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"✅ Concurrency resolved and lawyer profile updated for user: {userId}");
                    return true;
                }
            }

            _logger.LogInformation($"ℹ️ No changes detected for lawyer: {userId}");
            return true;
        }

        public async Task<string?> UploadProfilePictureAsync(Guid userId, IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension) || file.Length > 2 * 1024 * 1024) return null;

            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user == null) return null;
            if (user.Status != AccountStatus.Active) return null;

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "profiles", "lawyers");
            Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldFile = Path.Combine(_env.WebRootPath ?? "wwwroot", user.ProfilePicture.TrimStart('/'));
                if (File.Exists(oldFile)) File.Delete(oldFile);
            }

            var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }

            var pictureUrl = $"/profiles/lawyers/{fileName}";
            user.ProfilePicture = pictureUrl;
            await _context.SaveChangesAsync();

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";
            return $"{baseUrl}{pictureUrl}";
        }

        public async Task<bool> RemoveProfilePictureAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.ProfilePicture)) return false;

            var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", user.ProfilePicture.TrimStart('/'));
            if (File.Exists(filePath)) File.Delete(filePath);

            user.ProfilePicture = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LawyerProfileDto?> GetDashboardAsync(Guid userId)
        {
            return await GetProfileAsync(userId);
        }
    }
}