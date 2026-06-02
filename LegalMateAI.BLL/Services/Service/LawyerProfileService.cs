// LegalMateAI.BLL/Services/Service/LawyerProfileService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
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
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user == null || user.LawyerProfile == null) return null;

            var lawyerProfile = user.LawyerProfile;

            // فك تشفير البيانات بأمان
            string? decryptedLicense = DecryptSafely(lawyerProfile.LicenseNumber);
            string? decryptedPhone = DecryptSafely(user.Phone);
            string? decryptedAltPhone = DecryptSafely(user.UserProfile?.AlternativePhone);
            string? decryptedNationalId = DecryptSafely(user.NationalId);
            string? decryptedDateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd");

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

            // ✅ استخراج أسماء التخصصات كمصفوفة strings
            var specializationsList = lawyerProfile.Specialties?
                .Select(s => s.Specialty?.Name ?? s.Specialty?.NameAr ?? "")
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList() ?? new List<string>();

            _logger.LogInformation($"📊 Retrieved {specializationsList.Count} specializations for lawyer {userId}");
            _logger.LogInformation($"📊 Specializations: {string.Join(", ", specializationsList)}");

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
                GovernorateName = lawyerProfile.Governorate?.Name,
                City = lawyerProfile.City?.Name,
                OfficeAddress = lawyerProfile.OfficeAddress,
                Status = user.Status,
                VerifiedAt = lawyerProfile.VerifiedAt,
                RejectionReason = lawyerProfile.RejectionReason,
                SuspensionReason = user.SuspensionReason,
                SuspendedAt = user.SuspendedAt,
                ActivatedAt = user.ActivatedAt,
                ActiveCases = activeCases,
                UpcomingHearings = upcomingHearings,
                TotalClients = totalClients,
                CreatedAt = user.CreatedAt,
                Specializations = specializationsList
            };
        }

     // LegalMateAI.BLL/Services/Service/LawyerProfileService.cs
// جزء UpdateProfileAsync فقط - المعدل بالكامل

public async Task<bool> UpdateProfileAsync(Guid userId, UpdateLawyerProfileDto request)
{
    _logger.LogInformation($"🔵 UpdateProfileAsync called for lawyer: {userId}");

    var user = await _context.Users
        .Include(u => u.LawyerProfile)
            .ThenInclude(lp => lp!.Specializations)
        .Include(u => u.UserProfile)
        .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

    if (user == null || user.LawyerProfile == null)
    {
        _logger.LogWarning($"❌ User not found for userId: {userId}");
        return false;
    }

    var lawyerProfile = user.LawyerProfile;
    _logger.LogInformation($"✅ LawyerProfile found: Id={lawyerProfile.Id}, UserId={lawyerProfile.UserId}");
    
    bool hasChanges = false;

    // تحديث الاسم الأول
    if (!string.IsNullOrEmpty(request.FirstName))
    {
        user.FirstName = request.FirstName;
        hasChanges = true;
    }

    // تحديث الاسم الأخير
    if (!string.IsNullOrEmpty(request.LastName))
    {
        user.LastName = request.LastName;
        hasChanges = true;
    }

    // تحديث البريد الإلكتروني
    if (!string.IsNullOrEmpty(request.Email))
    {
        user.Email = request.Email;
        hasChanges = true;
    }

    // تحديث رقم الهاتف
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

    // تحديث الهاتف البديل
    if (request.AlternativePhone != null)
    {
        if (user.UserProfile == null)
        {
            user.UserProfile = new UserProfile { Id = Guid.NewGuid(), UserId = userId };
        }
        try
        {
            user.UserProfile.AlternativePhone = string.IsNullOrEmpty(request.AlternativePhone)
                ? null
                : _encryption.Encrypt(request.AlternativePhone);
        }
        catch
        {
            user.UserProfile.AlternativePhone = request.AlternativePhone;
        }
        hasChanges = true;
    }

    // تحديث الجنسية
    if (!string.IsNullOrEmpty(request.Nationality))
    {
        user.Nationality = request.Nationality;
        hasChanges = true;
    }

    // تحديث تاريخ الميلاد
    if (!string.IsNullOrEmpty(request.DateOfBirth))
    {
        if (DateTime.TryParse(request.DateOfBirth, out var dob))
        {
            user.DateOfBirth = dob;
            hasChanges = true;
        }
    }

    // تحديث الرقم القومي
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

    // تحديث سنوات الخبرة
    if (request.YearsOfExperience.HasValue)
    {
        lawyerProfile.YearsOfExperience = request.YearsOfExperience.Value;
        hasChanges = true;
    }

    // تحديث نقابة المحامين
    if (!string.IsNullOrEmpty(request.BarAssociation))
    {
        lawyerProfile.BarAssociation = request.BarAssociation;
        hasChanges = true;
    }

    // تحديث رقم الرخصة
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

    // تحديث تاريخ إصدار الرخصة
    if (request.LicenseIssueDate.HasValue)
    {
        lawyerProfile.LicenseIssueDate = request.LicenseIssueDate;
        hasChanges = true;
    }

    // تحديث درجة المزاولة
    if (!string.IsNullOrEmpty(request.PracticeDegree))
    {
        lawyerProfile.PracticeDegree = request.PracticeDegree;
        hasChanges = true;
    }

    // تحديث الموقع
    if (request.GovernorateId.HasValue)
    {
        lawyerProfile.GovernorateId = request.GovernorateId;
        hasChanges = true;
    }

    if (!string.IsNullOrEmpty(request.City))
    {
        var city = await _context.Cities.FirstOrDefaultAsync(c => c.Name == request.City);
        if (city != null)
            lawyerProfile.CityId = city.Id;
        hasChanges = true;
    }

    if (!string.IsNullOrEmpty(request.OfficeAddress))
    {
        lawyerProfile.OfficeAddress = request.OfficeAddress;
        hasChanges = true;
    }

    // ✅ تحديث التخصصات - النسخة المعدلة (باستخدام LawyerSpecializations)
    if (request.Specializations != null && request.Specializations.Any())
    {
        _logger.LogInformation($"📚 Updating specializations for lawyer {userId}: {string.Join(", ", request.Specializations)}");
        _logger.LogInformation($"📚 LawyerProfile.Id: {lawyerProfile.Id}");

        // حذف التخصصات القديمة من جدول تخصصات المحامي
        var oldCount = lawyerProfile.Specializations.Count;
        if (lawyerProfile.Specializations.Any())
        {
            _context.LawyerSpecializations.RemoveRange(lawyerProfile.Specializations);
            _logger.LogInformation($"🗑️ Removed {oldCount} old specializations");
        }

        // إضافة التخصصات الجديدة للمحامي
        foreach (var specName in request.Specializations)
        {
            if (string.IsNullOrWhiteSpace(specName)) continue;

            _logger.LogInformation($"🔍 Processing specialization: '{specName}'");

            // البحث عن التخصص في جدول التخصصات العامة
            var specialty = await _context.LawyerSpecialties
                .FirstOrDefaultAsync(s => s.Name == specName || s.NameAr == specName);

            if (specialty == null)
            {
                // إنشاء تخصص جديد في جدول التخصصات العامة
                specialty = new LawyerSpecialty
                {
                    Name = specName,
                    NameAr = specName,
                    IsActive = true
                };
                await _context.LawyerSpecialties.AddAsync(specialty);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"✨ Created new specialty: '{specName}' with ID: {specialty.Id}");
            }
            else
            {
                _logger.LogInformation($"✅ Found existing specialty: '{specName}' with ID: {specialty.Id}");
            }

            // ربط التخصص بالمحامي في جدول LawyerSpecializations
            var lawyerSpecialization = new LawyerSpecialization
            {
                Id = Guid.NewGuid(),
                LawyerId = lawyerProfile.Id,
                SpecializationId = specialty.Id,
                IsPrimary = false,
                CasesCount = 0
            };
            _context.LawyerSpecializations.Add(lawyerSpecialization);
            _logger.LogInformation($"🔗 Added relationship: LawyerId={lawyerProfile.Id}, SpecializationId={specialty.Id}");
        }
        hasChanges = true;
    }

    if (hasChanges)
    {
        lawyerProfile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation($"✅ Lawyer profile updated successfully for user: {userId}");
        return true;
    }

    _logger.LogInformation($"ℹ️ No changes detected for lawyer: {userId}");
    return true;
}        public async Task<string?> UploadProfilePictureAsync(Guid userId, IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension) || file.Length > 2 * 1024 * 1024) return null;

            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user == null) return null;

            if (user.Status != AccountStatus.Active)
                return null;

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

        // دالة آمنة لفك التشفير
        private string? DecryptSafely(string? encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return null;

            try
            {
                return _encryption.Decrypt(encrypted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Decryption failed, returning as plain text. Error: {ex.Message}");
                return encrypted;
            }
        }
    }
}