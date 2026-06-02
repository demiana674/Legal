// LegalMateAI.BLL/Services/Service/UserProfileService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Infrastructure.Services.IService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
                    .ThenInclude(up => up!.City)
                        .ThenInclude(c => c!.Governorate)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null) return null;

            // فك تشفير البيانات الحساسة
            string? decryptedPhone = Decrypt(user.Phone);
            string? decryptedNationalId = Decrypt(user.NationalId);
            string? decryptedAltPhone= null;
            
            
            if (user.UserProfile != null)
            {
                decryptedAltPhone = Decrypt(user.UserProfile.AlternativePhone);
            }

            return new UserProfileDto
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = decryptedPhone,
                NationalId = decryptedNationalId,
                Nationality = user.Nationality ?? "مصري",
                DateOfBirth = user.DateOfBirth?.ToString("dd MMMM yyyy"),
                // Gender = "ذكر",
                AlternativePhone = decryptedAltPhone,
                ProfilePicture = user.ProfilePicture,
                GovernorateName = user.UserProfile?.City?.Governorate?.Name,
                CityName = user.UserProfile?.City?.Name,
                Address = user.UserProfile?.Address,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                LastPasswordChange = "غير متاح"
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateUserProfileDto request)
        {
            _logger.LogInformation($"UpdateProfileAsync called for userId: {userId}");

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null) return false;

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.UserID,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserProfiles.Add(user.UserProfile);
            }

            var profile = user.UserProfile;
            bool hasChanges = false;

            // تشفير رقم الهاتف
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user.Phone = _encryption.Encrypt(request.PhoneNumber);
                profile.PhoneNumber = _encryption.Encrypt(request.PhoneNumber);
                hasChanges = true;
            }

            // تشفير الهاتف البديل
            if (request.AlternativePhone != null)
            {
                if (string.IsNullOrEmpty(request.AlternativePhone))
                    profile.AlternativePhone = null;
                else
                    profile.AlternativePhone = _encryption.Encrypt(request.AlternativePhone);
                hasChanges = true;
            }

            if (request.GovernorateId.HasValue)
            {
                hasChanges = true;
            }

            if (request.CityId.HasValue)
            {
                profile.CityId = request.CityId;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.Address))
            {
                profile.Address = request.Address;
                hasChanges = true;
            }

            if (hasChanges)
            {
                profile.UpdatedAt = DateTime.UtcNow;
                profile.LastProfileUpdate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Profile updated successfully for user {userId}");
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
            if (user == null) return null;

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "profiles", "users");
            Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldFile = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), user.ProfilePicture.TrimStart('/'));
                if (File.Exists(oldFile)) File.Delete(oldFile);
            }

            var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }

            var pictureUrl = $"/profiles/users/{fileName}";
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

            var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), user.ProfilePicture.TrimStart('/'));
            if (File.Exists(filePath)) File.Delete(filePath);

            user.ProfilePicture = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserProfileDto?> GetDashboardAsync(Guid userId)
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