// LegalMateAI.BLL/Services/Service/AdminProfileService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Infrastructure.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class AdminProfileService : IAdminProfileService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<AdminProfileService> _logger;

        public AdminProfileService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            IEncryptionService encryption,
            ILogger<AdminProfileService> logger)
        {
            _context = context;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _encryption = encryption;
            _logger = logger;
        }

        public async Task<AdminProfileDto?> GetProfileAsync(Guid adminId)
        {
            var admin = await _context.Admins
                .Include(a => a.Profile)
                    .ThenInclude(p => p!.Governorate)
                .Include(a => a.Profile)
                    .ThenInclude(p => p!.City)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin == null) return null;

            var profile = admin.Profile;
            var monthsActive = admin.CreatedAt != DateTime.MinValue
                ? (int)((DateTime.UtcNow - admin.CreatedAt).TotalDays / 30)
                : 0;

            // فك تشفير البيانات
            string? decryptedNationalId = null;
            if (!string.IsNullOrEmpty(profile?.NationalId))
            {
                try { decryptedNationalId = _encryption.Decrypt(profile.NationalId); }
                catch { decryptedNationalId = "غير متاح"; }
            }

            string? decryptedPhone = null;
            if (!string.IsNullOrEmpty(admin.PhoneNumber))
            {
                try { decryptedPhone = _encryption.Decrypt(admin.PhoneNumber); }
                catch { decryptedPhone = "غير متاح"; }
            }

            string? decryptedAltPhone = null;
            if (!string.IsNullOrEmpty(profile?.AlternativePhone))
            {
                try { decryptedAltPhone = _encryption.Decrypt(profile.AlternativePhone); }
                catch { decryptedAltPhone = "غير متاح"; }
            }

            return new AdminProfileDto
            {
                Id = admin.Id,
                FullName = admin.FullName,
                FirstName = profile?.FirstName,
                LastName = profile?.LastName,
                Email = admin.Email,
                PhoneNumber = decryptedPhone,
                AlternativePhone = decryptedAltPhone,
                ProfilePicture = profile?.ProfilePictureUrl,
                JobTitle = profile?.JobTitle,
                Department = profile?.Department,
                DateOfBirth = profile?.DateOfBirth?.ToString("yyyy-MM-dd"),
                Nationality = profile?.Nationality,
                NationalId = decryptedNationalId,
                Governorate = profile?.Governorate?.Name,
                City = profile?.City?.Name,
                Address = profile?.Address,
                CreatedAt = admin.CreatedAt,
                LastLoginAt = admin.LastLoginAt,
                JoinDateFormatted = profile?.JoinDate?.ToString("MMMM yyyy", new System.Globalization.CultureInfo("ar-EG")),
                TotalMonthsActive = monthsActive,
                Status = "نشط",
                IsOnline = admin.LastLoginAt?.Date == DateTime.UtcNow.Date,
                TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User),
                TotalLawyers = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer),
                PendingVerifications = await _context.LawyerProfiles.CountAsync(l => l.VerificationStatus == LawyerVerificationStatus.Pending),
                VerifiedToday = await _context.LawyerProfiles.CountAsync(l => l.VerifiedAt != null && l.VerifiedAt.Value.Date == DateTime.UtcNow.Date),
                TotalVerifiedLawyers = profile?.TotalVerifiedLawyers ?? 0,
                TotalRejectedLawyers = profile?.TotalRejectedLawyers ?? 0,
                CanManageUsers = true,
                CanVerifyLawyers = true,
                CanManageSystem = true,
                CanExportData = true
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid adminId, UpdateAdminProfileDto request)
        {
            var admin = await _context.Admins
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin == null) return false;

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                admin.PhoneNumber = _encryption.Encrypt(request.PhoneNumber);
                if (admin.Profile != null)
                {
                    admin.Profile.LastActiveAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<string?> UploadProfilePictureAsync(Guid adminId, IFormFile file)
        {
            if (file == null || file.Length == 0) return null;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension) || file.Length > 2 * 1024 * 1024) return null;

            var admin = await _context.Admins.Include(a => a.Profile).FirstOrDefaultAsync(a => a.Id == adminId);
            if (admin == null) return null;

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "profiles", "admins");
            Directory.CreateDirectory(uploadsFolder);

            if (admin.Profile?.ProfilePictureUrl != null)
            {
                var oldFile = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), admin.Profile.ProfilePictureUrl.TrimStart('/'));
                if (File.Exists(oldFile)) File.Delete(oldFile);
            }

            var fileName = $"{adminId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }

            var pictureUrl = $"/profiles/admins/{fileName}";
            if (admin.Profile == null)
            {
                admin.Profile = new AdminProfile
                {
                    Id = Guid.NewGuid(),
                    AdminId = admin.Id,
                    ProfilePictureUrl = pictureUrl,
                    LastActiveAt = DateTime.UtcNow
                };
                _context.AdminProfiles.Add(admin.Profile);
            }
            else
            {
                admin.Profile.ProfilePictureUrl = pictureUrl;
                admin.Profile.LastActiveAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";
            return $"{baseUrl}{pictureUrl}";
        }

        public async Task<bool> RemoveProfilePictureAsync(Guid adminId)
        {
            var admin = await _context.Admins.Include(a => a.Profile).FirstOrDefaultAsync(a => a.Id == adminId);
            if (admin?.Profile?.ProfilePictureUrl == null) return false;

            var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), admin.Profile.ProfilePictureUrl.TrimStart('/'));
            if (File.Exists(filePath)) File.Delete(filePath);

            admin.Profile.ProfilePictureUrl = null;
            admin.Profile.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AdminDashboardDto?> GetDashboardAsync(Guid adminId)
        {
            var admin = await _context.Admins.Include(a => a.Profile).FirstOrDefaultAsync(a => a.Id == adminId);
            if (admin == null) return null;

            if (admin.Profile != null)
            {
                admin.Profile.LastActiveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var pendingLawyers = await _context.Users
                .Include(u => u.LawyerProfile)
                .Where(u => u.Role == UserRole.Lawyer && u.LawyerProfile != null &&
                       u.LawyerProfile.VerificationStatus == LawyerVerificationStatus.Pending)
                .OrderBy(u => u.CreatedAt)
                .Take(10)
                .Select(u => new PendingLawyerDto
                {
                    UserId = u.UserID,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Phone = u.Phone ?? "",
                    LicenseNumber = u.LawyerProfile!.LicenseNumber ?? "",
                    BarAssociation = u.LawyerProfile.BarAssociation ?? "",
                    YearsOfExperience = u.LawyerProfile.YearsOfExperience ?? 0,
                    RegisteredAt = u.CreatedAt
                }).ToListAsync();

            return new AdminDashboardDto
            {
                TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User),
                TotalLawyers = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer),
                PendingVerifications = pendingLawyers.Count,
                VerifiedToday = await _context.LawyerProfiles.CountAsync(l => l.VerifiedAt != null && l.VerifiedAt.Value.Date == DateTime.UtcNow.Date),
                PendingLawyers = pendingLawyers,
                RecentActivity = new(),
                AdminName = admin.FullName,
                ProfilePicture = admin.Profile?.ProfilePictureUrl,
                JobTitle = admin.Profile?.JobTitle ?? "مدير النظام"
            };
        }

        private string? Decrypt(string? encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return null;
            try { return _encryption.Decrypt(encrypted); }
            catch { return "غير متاح"; }
        }
    }
}