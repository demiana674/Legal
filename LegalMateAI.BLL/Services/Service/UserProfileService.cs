// LegalMateAI.BLL/Services/Service/UserProfileService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using LegalMateAI.Infrastructure.Services.IService;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class UserProfileService : IUserProfileService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            IEncryptionService encryption,
            ILogger<UserProfileService> logger)
        {
            _context = context;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _encryption = encryption;
            _logger = logger;
        }

        public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.Governorate)
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.City)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null) return null;

            string? decryptedPhone = null;
            if (!string.IsNullOrEmpty(user.Phone))
            {
                try
                {
                    decryptedPhone = _encryption.Decrypt(user.Phone);
                }
                catch
                {
                    decryptedPhone = "غير متاح";
                }
            }

            string? decryptedAlternativePhone = null;
            if (user.UserProfile != null && !string.IsNullOrEmpty(user.UserProfile.AlternativePhone))
            {
                try
                {
                    decryptedAlternativePhone = _encryption.Decrypt(user.UserProfile.AlternativePhone);
                }
                catch
                {
                    decryptedAlternativePhone = "غير متاح";
                }
            }

            string? decryptedNationalId = null;
            if (!string.IsNullOrEmpty(user.NationalId))
            {
                try
                {
                    decryptedNationalId = _encryption.Decrypt(user.NationalId);
                }
                catch
                {
                    decryptedNationalId = "غير متاح";
                }
            }

            return new UserProfileDto
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = decryptedPhone,
                AlternativePhone = decryptedAlternativePhone,
                NationalId = decryptedNationalId,
                ProfilePicture = user.ProfilePicture,
                GovernorateName = user.UserProfile?.Governorate?.Name,
                CityName = user.UserProfile?.City?.Name,
                Address = user.UserProfile?.Address,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateUserProfileDto request)
        {
            _logger.LogInformation($"UpdateProfileAsync called for userId: {userId}");

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return false;
            }

            // ✅ التحقق من وجود UserProfile - إذا لم يكن موجوداً، أرسل رسالة خطأ
            if (user.UserProfile == null)
            {
                _logger.LogWarning($"UserProfile not found for user: {userId}");
                return false;
            }

            bool hasChanges = false;

            // تحديث رقم الهاتف الأساسي
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user.Phone = _encryption.Encrypt(request.PhoneNumber);
                hasChanges = true;
                _logger.LogInformation($"Phone number updated for user {userId}");
            }

            // تحديث رقم الهاتف البديل (في UserProfile)
            if (!string.IsNullOrEmpty(request.AlternativePhone))
            {
                user.UserProfile.AlternativePhone = _encryption.Encrypt(request.AlternativePhone);
                hasChanges = true;
                _logger.LogInformation($"Alternative phone updated for user {userId}");
            }
            else if (request.AlternativePhone == "")
            {
                user.UserProfile.AlternativePhone = null;
                hasChanges = true;
            }

            // تحديث المحافظة (في UserProfile)
            if (request.GovernorateId.HasValue)
            {
                user.UserProfile.GovernorateId = request.GovernorateId;
                hasChanges = true;
                _logger.LogInformation($"GovernorateId updated for user {userId}");
            }

            // تحديث المدينة (في UserProfile)
            if (request.CityId.HasValue)
            {
                user.UserProfile.CityId = request.CityId;
                hasChanges = true;
                _logger.LogInformation($"CityId updated for user {userId}");
            }

            // تحديث العنوان
            if (!string.IsNullOrEmpty(request.Address))
            {
                user.UserProfile.Address = request.Address;
                hasChanges = true;
                _logger.LogInformation($"Address updated for user {userId}");
            }

            if (hasChanges)
            {
                user.UserProfile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Profile updated successfully for user {userId}");
                return true;
            }

            _logger.LogInformation($"No changes detected for user {userId}");
            return true;
        }

        public async Task<string?> UploadProfilePictureAsync(Guid userId, IFormFile file)
        {
            _logger.LogInformation($"UploadProfilePictureAsync called for userId: {userId}");

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file provided");
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

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return null;
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "profiles", "users");
            Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldFile = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), user.ProfilePicture.TrimStart('/'));
                if (File.Exists(oldFile))
                {
                    File.Delete(oldFile);
                    _logger.LogInformation($"Deleted old profile picture: {oldFile}");
                }
            }

            var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var pictureUrl = $"/profiles/users/{fileName}";
            user.ProfilePicture = pictureUrl;
            await _context.SaveChangesAsync();

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";
            var fullUrl = $"{baseUrl}{pictureUrl}";

            _logger.LogInformation($"Profile picture uploaded successfully: {fullUrl}");
            return fullUrl;
        }

        public async Task<bool> RemoveProfilePictureAsync(Guid userId)
        {
            _logger.LogInformation($"RemoveProfilePictureAsync called for userId: {userId}");

            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.ProfilePicture))
            {
                _logger.LogWarning($"No profile picture to remove for user {userId}");
                return false;
            }

            var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), user.ProfilePicture.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation($"Deleted profile picture file: {filePath}");
            }

            user.ProfilePicture = null;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Profile picture removed successfully for user {userId}");
            return true;
        }

        public async Task<UserProfileDto?> GetDashboardAsync(Guid userId)
        {
            return await GetProfileAsync(userId);
        }
    }
}