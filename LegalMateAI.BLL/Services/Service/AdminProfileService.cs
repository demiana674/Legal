// LegalMateAI.BLL/Services/Service/AdminProfileService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
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

        public async Task<AdminProfileDto?> GetProfileAsync(Guid adminId)
        {
            _logger.LogInformation($"GetProfileAsync called with adminId: {adminId}");
            
            var admin = await _context.Admins
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin == null)
            {
                _logger.LogWarning($"Admin not found with ID: {adminId}");
                return null;
            }

            string? decryptedPhone = null;
            if (!string.IsNullOrEmpty(admin.PhoneNumber))
            {
                try
                {
                    decryptedPhone = _encryption.Decrypt(admin.PhoneNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decrypt phone number for admin: {AdminId}", adminId);
                    decryptedPhone = "غير متاح";
                }
            }

            return new AdminProfileDto
            {
                Id = admin.Id,
                FullName = admin.FullName,
                Email = admin.Email,
                PhoneNumber = decryptedPhone,
                ProfilePicture = admin.Profile?.ProfilePictureUrl,
                JobTitle = admin.Profile?.JobTitle,
                CreatedAt = admin.CreatedAt,
                LastLoginAt = admin.LastLoginAt,
                
                TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User),
                TotalLawyers = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer),
                PendingVerifications = await _context.LawyerProfiles
                    .CountAsync(l => l.VerificationStatus == LawyerVerificationStatus.Pending),
                VerifiedToday = await _context.LawyerProfiles
                    .CountAsync(l => l.VerifiedAt != null && l.VerifiedAt.Value.Date == DateTime.UtcNow.Date)
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid adminId, UpdateAdminProfileDto request)
        {
            _logger.LogInformation($"UpdateProfileAsync called with adminId: {adminId}");
            
            var admin = await _context.Admins
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin == null)
            {
                _logger.LogWarning($"Admin not found with ID: {adminId}");
                return false;
            }

            bool hasChanges = false;


            // ✅ تحديث رقم الهاتف
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                admin.PhoneNumber = _encryption.Encrypt(request.PhoneNumber);
                hasChanges = true;
                _logger.LogInformation($"Phone number updated for admin {adminId}");
            }


            if (hasChanges)
            {
                // ✅ EF Core يتتبع التغييرات تلقائياً - لا نحتاج لـ EntityState.Modified
                var savedRows = await _context.SaveChangesAsync();
                _logger.LogInformation($"SaveChangesAsync affected {savedRows} row(s) for admin {adminId}");
                
                if (savedRows > 0)
                {
                    _logger.LogInformation($"✅ Admin profile updated successfully for {adminId}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"⚠️ No changes were saved for admin {adminId} (hasChanges=true but savedRows=0)");
                    return false;
                }
            }

            _logger.LogInformation($"No changes detected for admin {adminId}");
            return true;
        }

        public async Task<string?> UploadProfilePictureAsync(Guid adminId, IFormFile file)
        {
            _logger.LogInformation($"UploadProfilePictureAsync called with adminId: {adminId}, File: {file?.FileName}");

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file provided for upload");
                return null;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning($"Invalid file extension: {extension}");
                return null;
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                _logger.LogWarning($"File too large: {file.Length} bytes");
                return null;
            }

            var admin = await _context.Admins
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin == null)
            {
                _logger.LogWarning($"Admin not found with ID: {adminId}");
                return null;
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "profiles", "admins");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
                _logger.LogInformation($"Created directory: {uploadsFolder}");
            }

            // حذف الصورة القديمة
            if (admin.Profile?.ProfilePictureUrl != null)
            {
                var oldFile = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), 
                    admin.Profile.ProfilePictureUrl.TrimStart('/'));
                if (File.Exists(oldFile))
                {
                    File.Delete(oldFile);
                    _logger.LogInformation($"Deleted old profile picture: {oldFile}");
                }
            }

            // حفظ الصورة الجديدة
            var fileName = $"{adminId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            _logger.LogInformation($"Saved new profile picture: {filePath}");

            var pictureUrl = $"/profiles/admins/{fileName}";
            
            // إنشاء أو تحديث AdminProfile
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
                _logger.LogInformation($"Created new AdminProfile for admin {adminId}");
            }
            else
            {
                admin.Profile.ProfilePictureUrl = pictureUrl;
                admin.Profile.LastActiveAt = DateTime.UtcNow;
                _logger.LogInformation($"Updated existing AdminProfile for admin {adminId}");
            }

            // ✅ EF Core يتتبع التغييرات تلقائياً
            var savedRows = await _context.SaveChangesAsync();
            _logger.LogInformation($"SaveChangesAsync affected {savedRows} row(s) for admin {adminId}");

            if (savedRows > 0)
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                var baseUrl = $"{request?.Scheme}://{request?.Host}";
                var fullUrl = $"{baseUrl}{pictureUrl}";
                _logger.LogInformation($"Profile picture uploaded successfully: {fullUrl}");
                return fullUrl;
            }

            _logger.LogWarning($"Failed to save profile picture for admin {adminId}");
            return null;
        }

        public async Task<bool> RemoveProfilePictureAsync(Guid adminId)
        {
            _logger.LogInformation($"RemoveProfilePictureAsync called with adminId: {adminId}");

            var admin = await _context.Admins
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin?.Profile?.ProfilePictureUrl == null)
            {
                _logger.LogWarning($"No profile picture to remove for admin {adminId}");
                return false;
            }

            // حذف الملف الفعلي
            var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), 
                admin.Profile.ProfilePictureUrl.TrimStart('/'));
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation($"Deleted profile picture file: {filePath}");
            }

            // تحديث قاعدة البيانات
            admin.Profile.ProfilePictureUrl = null;
            admin.Profile.LastActiveAt = DateTime.UtcNow;
            
            // ✅ EF Core يتتبع التغييرات تلقائياً
            var savedRows = await _context.SaveChangesAsync();
            _logger.LogInformation($"SaveChangesAsync affected {savedRows} row(s) for admin {adminId}");

            if (savedRows > 0)
            {
                _logger.LogInformation($"Profile picture removed successfully for admin {adminId}");
                return true;
            }

            _logger.LogWarning($"Failed to remove profile picture for admin {adminId}");
            return false;
        }

        public async Task<AdminDashboardDto?> GetDashboardAsync(Guid adminId)
        {
            _logger.LogInformation($"GetDashboardAsync called with adminId: {adminId}");

            var admin = await _context.Admins
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Id == adminId);
                
            if (admin == null)
            {
                _logger.LogWarning($"Admin not found with ID: {adminId}");
                return null;
            }

            _logger.LogInformation($"Getting dashboard for admin: {admin.Email} (ID: {adminId})");

            // ✅ فقط المحامين المنتظرين
            var pendingLawyers = await _context.Users
                .Include(u => u.LawyerProfile)
                .Where(u => u.Role == UserRole.Lawyer && 
                            u.LawyerProfile != null &&
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
                })
                .ToListAsync();

            _logger.LogInformation($"Found {pendingLawyers.Count} pending lawyers");

            // ✅ فقط أنشطة هذا الأدمن (وليس كل الأدمن)
            var recentActivity = await _context.AdminLogs
                .Include(l => l.Admin)
                .Where(l => l.AdminId == adminId)
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .Select(l => new AdminLogDto
                {
                    Id = l.Id,
                    AdminName = l.Admin != null ? l.Admin.FullName : admin.FullName,
                    Action = l.Action,
                    TargetType = l.TargetType,
                    TargetId = l.TargetId,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();

            _logger.LogInformation($"Found {recentActivity.Count} recent activities for admin {adminId}");

            return new AdminDashboardDto
            {
                TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User),
                TotalLawyers = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer),
                PendingVerifications = pendingLawyers.Count,
                VerifiedToday = await _context.LawyerProfiles
                    .CountAsync(l => l.VerifiedAt != null && l.VerifiedAt.Value.Date == DateTime.UtcNow.Date),
                PendingLawyers = pendingLawyers,
                RecentActivity = recentActivity,
                AdminName = admin.FullName,
                ProfilePicture = admin.Profile?.ProfilePictureUrl,
                JobTitle = admin.Profile?.JobTitle ?? "مدير النظام"
            };
        }
    }
}