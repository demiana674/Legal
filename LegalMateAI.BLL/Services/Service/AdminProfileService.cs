using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using LegalMateAI.Infrastructure.Services.IService;
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

        private string? Decrypt(string? encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return null;
            
            try
            {
                return _encryption.Decrypt(encryptedText);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt value");
                return encryptedText;
            }
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

            return new AdminProfileDto
            {
                Id = admin.Id,
                FullName = admin.FullName,
                FirstName = profile?.FirstName,
                LastName = profile?.LastName,
                Email = admin.Email,
                PhoneNumber = Decrypt(admin.PhoneNumber),
                AlternativePhone = Decrypt(profile?.AlternativePhone),
                ProfilePicture = profile?.ProfilePictureUrl,
                JobTitle = profile?.JobTitle,
                Department = profile?.Department,
                DateOfBirth = profile?.DateOfBirth?.ToString("yyyy-MM-dd"),
                Nationality = profile?.Nationality,
                NationalId = Decrypt(profile?.NationalId),
                Governorate = profile?.Governorate?.Name ?? "",
                City = profile?.City?.Name ?? "",
                Address = profile?.Address,
                CreatedAt = admin.CreatedAt,
                LastLoginAt = admin.LastLoginAt,
                JoinDateFormatted = profile?.JoinDate?.ToString("MMMM yyyy", new System.Globalization.CultureInfo("ar-EG")),
                Status = "نشط",
                IsOnline = admin.LastLoginAt?.Date == DateTime.UtcNow.Date,
                
                // ✅ التعديل: استخدام User.Status بدلاً من LawyerProfile.VerificationStatus
                TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User && u.Status == AccountStatus.Active),
                TotalLawyers = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer && u.Status == AccountStatus.Active),
                
                // ✅ المحامين المعلقين (Pending) من User.Status
                PendingVerifications = await _context.Users
                    .CountAsync(u => u.Role == UserRole.Lawyer && u.Status == AccountStatus.Pending),
                
                // ✅ تم توثيقهم اليوم من User.ActivatedAt
                VerifiedToday = await _context.Users
                    .CountAsync(u => u.Role == UserRole.Lawyer && u.ActivatedAt != null && u.ActivatedAt.Value.Date == DateTime.UtcNow.Date),
                
                TotalVerifiedLawyers = profile?.TotalVerifiedLawyers ?? 0,
                TotalRejectedLawyers = profile?.TotalRejectedLawyers ?? 0,
                CanVerifyLawyers = true,
                CanExportData = true
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid adminId, UpdateAdminProfileDto request)
        {
            var admin = await _context.Admins
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin == null) return false;

            // تحديث رقم الهاتف الأساسي
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                admin.PhoneNumber = _encryption.Encrypt(request.PhoneNumber);
            }

            // تحديث البيانات في الـ Profile إذا كان موجود
            if (admin.Profile != null)
            {
                if (!string.IsNullOrEmpty(request.AlternativePhone))
                {
                    admin.Profile.AlternativePhone = _encryption.Encrypt(request.AlternativePhone);
                }

                if (!string.IsNullOrEmpty(request.Address))
                {
                    admin.Profile.Address = request.Address;
                }

                if (request.GovernorateId.HasValue)
                {
                    admin.Profile.GovernorateId = request.GovernorateId.Value;
                }

                if (request.CityId.HasValue)
                {
                    admin.Profile.CityId = request.CityId.Value;
                }

                admin.Profile.LastActiveAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
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

            // ✅ التعديل: استخدام User.Status بدلاً من LawyerProfile.VerificationStatus
            var pendingLawyers = await _context.Users
                .Include(u => u.LawyerProfile)
                .Where(u => u.Role == UserRole.Lawyer && u.LawyerProfile != null &&
                       u.Status == AccountStatus.Pending)
                .OrderBy(u => u.CreatedAt)
                .Take(10)
                .Select(u => new PendingLawyerDto
                {
                    UserId = u.UserID,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Phone = Decrypt(u.Phone) ?? "",
                    LicenseNumber = Decrypt(u.LawyerProfile!.LicenseNumber) ?? "",
                    BarAssociation = u.LawyerProfile.BarAssociation ?? "",
                    YearsOfExperience = u.LawyerProfile.YearsOfExperience ?? 0,
                    RegisteredAt = u.CreatedAt
                }).ToListAsync();

            var recentActivity = await _context.AdminLogs
                .Where(l => l.ActorId == adminId)
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .Select(l => new AdminLogDto
                {
                    Id = l.Id,
                    Name = l.ActorName,
                    Action = l.Action,
                    TargetType = l.TargetType,
                    TargetId = l.TargetId,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();

            return new AdminDashboardDto
            {
                // ✅ التعديل: استخدام User.Status
                TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User && u.Status == AccountStatus.Active),
                TotalLawyers = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer && u.Status == AccountStatus.Active),
                PendingVerifications = pendingLawyers.Count,
                
                // ✅ التعديل: استخدام User.ActivatedAt
                VerifiedToday = await _context.Users
                    .CountAsync(u => u.Role == UserRole.Lawyer && u.ActivatedAt != null && u.ActivatedAt.Value.Date == DateTime.UtcNow.Date),
                
                PendingLawyers = pendingLawyers,
                RecentActivity = recentActivity,
                AdminName = admin.FullName,
                ProfilePicture = admin.Profile?.ProfilePictureUrl,
                JobTitle = admin.Profile?.JobTitle ?? "مدير النظام"
            };
        }
    }
}