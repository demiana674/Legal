// LegalMateAI.BLL/Services/Service/AdminService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Infrastructure.Services.IService;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class AdminService : IAdminService
    {
        private readonly LegalMateDbContext _context;
        private readonly PdfGenerationService _pdfService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AdminService> _logger;
        private readonly IEncryptionService _encryption;

        public AdminService(
            LegalMateDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AdminService> logger,
            IEncryptionService encryption)
        {
            _context = context;
            _pdfService = new PdfGenerationService();
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _encryption = encryption;
        }

        public async Task<AdminDashboardDto> GetDashboardStatsAsync(Guid adminId)
        {
            var today = DateTime.UtcNow.Date;
            var admin = await _context.Admins
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin?.Profile != null)
            {
                admin.Profile.LastActiveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new AdminDashboardDto
            {
                TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User),
                TotalLawyers = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer),
                PendingVerifications = await _context.LawyerProfiles.CountAsync(l => l.VerificationStatus == LawyerVerificationStatus.Pending),
                VerifiedToday = await _context.LawyerProfiles.CountAsync(l => l.VerifiedAt.HasValue && l.VerifiedAt.Value.Date == today),
                AdminName = admin?.FullName ?? "",
                ProfilePicture = admin?.Profile?.ProfilePictureUrl,
                JobTitle = admin?.Profile?.JobTitle ?? "مدير النظام",
                PendingLawyers = await GetPendingLawyersAsync(),
                RecentActivity = await GetLogsAsync(new LogFilterDto { PageSize = 10 })
            };
        }

        public async Task<List<UserResponseDto>> GetAllUsersAsync(UserFilterDto? filter = null)
        {
            var query = _context.Users
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.City)
                        .ThenInclude(c => c!.Governorate)
                .Where(u => u.Role == UserRole.User).AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<AccountStatus>(filter.Status, true, out var status))
                    query = query.Where(u => u.Status == status);
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var term = filter.SearchTerm.ToLower();
                    query = query.Where(u => u.FirstName.ToLower().Contains(term) || u.LastName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
                }
                if (filter.FromDate.HasValue) query = query.Where(u => u.CreatedAt >= filter.FromDate.Value);
                if (filter.ToDate.HasValue) query = query.Where(u => u.CreatedAt <= filter.ToDate.Value);
            }

            int page = Math.Max(1, filter?.Page ?? 1);
            int pageSize = Math.Max(1, Math.Min(100, filter?.PageSize ?? 20));

            return (await query.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()).Select(MapUserToDto).ToList();
        }

        public async Task<bool> UpdateUserStatusAsync(Guid adminId, Guid userId, AccountStatus status, string? reason = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return false;

            user.Status = status;
            user.IsActive = status == AccountStatus.Active;

            await UpdateAdminLastActive(adminId);
            await LogAdminActionAsync(adminId, status == AccountStatus.Suspended ? AdminLogAction.Suspend : AdminLogAction.Activate, "User", userId);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid adminId, Guid userId)
        {
            var user = await _context.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.User);
            if (user == null) return false;

            if (user.UserProfile != null) _context.UserProfiles.Remove(user.UserProfile);
            _context.Users.Remove(user);

            await UpdateAdminLastActive(adminId);
            await LogAdminActionAsync(adminId, AdminLogAction.Delete, "User", userId);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<PendingLawyerDto>> GetPendingLawyersAsync()
        {
            return await _context.Users.Include(u => u.LawyerProfile)
                .Where(u => u.Role == UserRole.Lawyer && u.LawyerProfile != null && u.LawyerProfile.VerificationStatus == LawyerVerificationStatus.Pending)
                .OrderBy(u => u.CreatedAt)
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
        }

        public async Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null)
        {
            var query = _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(s => s.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Governorate)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .Where(u => u.Role == UserRole.Lawyer && u.LawyerProfile != null).AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<LawyerVerificationStatus>(filter.Status, true, out var status))
                    query = query.Where(u => u.LawyerProfile!.VerificationStatus == status);
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var term = filter.SearchTerm.ToLower();
                    query = query.Where(u => u.FirstName.ToLower().Contains(term) || u.LastName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
                }
            }

            int page = Math.Max(1, filter?.Page ?? 1);
            int pageSize = Math.Max(1, Math.Min(100, filter?.PageSize ?? 20));

            return (await query.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()).Select(MapLawyerToDto).ToList();
        }

        public async Task<bool> ApproveLawyerAsync(Guid userId) => await UpdateLawyerStatusAsync(userId, LawyerVerificationStatus.Active);
        public async Task<bool> RejectLawyerAsync(Guid userId, string reason) => await UpdateLawyerStatusAsync(userId, LawyerVerificationStatus.Deactivated, reason);
        public async Task<bool> SuspendLawyerAsync(Guid userId, string? reason = null) => await UpdateLawyerStatusAsync(userId, LawyerVerificationStatus.Suspended, reason);
        public async Task<bool> ActivateLawyerAsync(Guid userId) => await UpdateLawyerStatusAsync(userId, LawyerVerificationStatus.Active);

        public async Task<bool> DeleteLawyerAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .ThenInclude(lp => lp!.Specialties)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);
            
            if (user == null) return false;

            if (user.LawyerProfile != null)
            {
                if (user.LawyerProfile.Specialties.Any()) 
                    _context.LawyerProfileSpecialties.RemoveRange(user.LawyerProfile.Specialties);
                _context.LawyerProfiles.Remove(user.LawyerProfile);
            }

            _context.Users.Remove(user);

            var adminId = GetCurrentAdminId();
            if (adminId.HasValue)
            {
                await UpdateAdminLastActive(adminId.Value);
                await LogAdminActionAsync(adminId.Value, AdminLogAction.Delete, "Lawyer", userId);
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<object?> GetEntityDetailsAsync(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.City)
                        .ThenInclude(c => c!.Governorate)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(s => s.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Governorate)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Certificates)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user != null)
            {
                if (user.Role == UserRole.Lawyer && user.LawyerProfile != null) 
                    return MapLawyerToDto(user);
                    
                var dto = MapUserToDto(user);
                if (user.UserProfile != null) 
                {
                    dto.City = user.UserProfile.City?.Name;
                    dto.Address = user.UserProfile.Address;
                }
                dto.DocumentsCount = await _context.Documents.CountAsync(d => d.UserId == id);
                dto.ContractsCount = await _context.Contracts.CountAsync(c => c.UserId == id);
                dto.AppointmentsCount = await _context.Appointments.CountAsync(a => a.UserID == id);
                return dto;
            }

            return await GetAdminDetailsAsync(id);
        }

        public async Task<AdminProfileDto?> GetAdminDetailsAsync(Guid adminId)
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
                Governorate = profile?.Governorate?.Name, 
                City = profile?.City?.Name, 
                Address = profile?.Address,
                CreatedAt = admin.CreatedAt, 
                LastLoginAt = admin.LastLoginAt,
                TotalVerifiedLawyers = profile?.TotalVerifiedLawyers ?? 0, 
                TotalRejectedLawyers = profile?.TotalRejectedLawyers ?? 0
            };
        }

        public async Task<List<AdminLogDto>> GetLogsAsync(LogFilterDto? filter = null)
        {
            var query = _context.AdminLogs.Include(l => l.Admin).AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.AdminId) && Guid.TryParse(filter.AdminId, out var adminId)) 
                    query = query.Where(l => l.AdminId == adminId);
                if (filter.UserId.HasValue) 
                    query = query.Where(l => l.TargetId == filter.UserId.Value);
                if (filter.Action.HasValue) 
                    query = query.Where(l => l.Action == filter.Action.Value);
                if (!string.IsNullOrEmpty(filter.TargetType)) 
                    query = query.Where(l => l.TargetType == filter.TargetType);
                if (!string.IsNullOrEmpty(filter.UserType)) 
                    query = query.Where(l => l.TargetType == filter.UserType);
                if (filter.FromDate.HasValue) 
                    query = query.Where(l => l.Timestamp >= filter.FromDate.Value);
                if (filter.ToDate.HasValue) 
                    query = query.Where(l => l.Timestamp <= filter.ToDate.Value);
            }

            int page = Math.Max(1, filter?.Page ?? 1);
            int pageSize = Math.Max(1, Math.Min(500, filter?.PageSize ?? 50));

            return (await query.OrderByDescending(l => l.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()).Select(MapLogToDto).ToList();
        }

        public async Task<byte[]> ExportLogsAsync(LogFilterDto? filter, string format = "csv")
        {
            var logs = await GetLogsAsync(filter);
            string adminName = "مدير النظام";
            if (!string.IsNullOrEmpty(filter?.AdminId) && Guid.TryParse(filter.AdminId, out var id)) 
            { 
                var a = await _context.Admins.FindAsync(id); 
                if (a != null) adminName = a.FullName; 
            }
            return format.ToLower() == "pdf" ? _pdfService.ExportAdminLogsToPdf(logs, adminName) : _pdfService.ExportAdminLogsToExcel(logs);
        }

        public async Task<SystemLogsStatsDto> GetLogsStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var allLogs = await _context.AdminLogs.ToListAsync();
            return new SystemLogsStatsDto
            {
                TotalLogs = allLogs.Count, 
                TodayLogs = allLogs.Count(l => l.Timestamp.Date == today),
                LawyersVerified = allLogs.Count(l => l.Action == AdminLogAction.Verify),
                LawyersRejected = allLogs.Count(l => l.Action == AdminLogAction.Reject),
                UsersRegistered = await _context.Users.CountAsync(u => u.CreatedAt.Date == today),
                AdminActions = allLogs.Count(l => l.Action != AdminLogAction.Login),
                LastActivityAt = allLogs.OrderByDescending(l => l.Timestamp).FirstOrDefault()?.Timestamp,
                ActionsByType = allLogs.GroupBy(l => l.Action.ToString()).ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<SystemStatsDto> GetSystemStatsAsync()
        {
            return new SystemStatsDto
            {
                TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User),
                TotalLawyers = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer),
                TotalAdmins = await _context.Admins.CountAsync(),
                TotalDocuments = await _context.Documents.CountAsync(),
                TotalContracts = await _context.Contracts.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                PendingVerifications = await _context.LawyerProfiles.CountAsync(l => l.VerificationStatus == LawyerVerificationStatus.Pending),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive && u.Role == UserRole.User),
                ActiveLawyers = await _context.Users.CountAsync(u => u.IsActive && u.Role == UserRole.Lawyer),
            };
        }

        public Task<bool> ClearCacheAsync(Guid adminId) => Task.FromResult(true);

        // ===== Private Helpers =====
        private async Task UpdateAdminLastActive(Guid adminId)
        {
            var adminProfile = await _context.AdminProfiles.FirstOrDefaultAsync(ap => ap.AdminId == adminId);
            if (adminProfile != null) 
            { 
                adminProfile.LastActiveAt = DateTime.UtcNow; 
            }
        }

        private async Task LogAdminActionAsync(Guid adminId, AdminLogAction action, string targetType, Guid? targetId)
        {
            await UpdateAdminLastActive(adminId);
            _context.AdminLogs.Add(new AdminLog 
            { 
                Id = Guid.NewGuid(), 
                AdminId = adminId, 
                Action = action, 
                TargetType = targetType, 
                TargetId = targetId ?? Guid.Empty, 
                Timestamp = DateTime.UtcNow 
            });
            await _context.SaveChangesAsync();
        }

        private async Task<bool> UpdateLawyerStatusAsync(Guid userId, LawyerVerificationStatus status, string? notes = null)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);
                
            if (user?.LawyerProfile == null) return false;

            user.LawyerProfile.VerificationStatus = status;
            switch (status)
            {
                case LawyerVerificationStatus.Active: 
                    user.IsActive = true; 
                    user.Status = AccountStatus.Active; 
                    user.LawyerProfile.VerifiedAt = DateTime.UtcNow; 
                    user.LawyerProfile.RejectionReason = null; 
                    break;
                case LawyerVerificationStatus.Suspended: 
                    user.IsActive = false; 
                    user.Status = AccountStatus.Suspended; 
                    user.LawyerProfile.RejectionReason = notes; 
                    break;
                case LawyerVerificationStatus.Deactivated: 
                    user.IsActive = false; 
                    user.Status = AccountStatus.Deactivated; 
                    user.LawyerProfile.RejectionReason = notes; 
                    break;
            }

            var adminId = GetCurrentAdminId();
            if (adminId.HasValue)
            {
                var adminProfile = await _context.AdminProfiles.FirstOrDefaultAsync(ap => ap.AdminId == adminId.Value);
                if (adminProfile != null)
                {
                    adminProfile.LastActiveAt = DateTime.UtcNow;
                    if (status == LawyerVerificationStatus.Active) adminProfile.TotalVerifiedLawyers++;
                    else if (status == LawyerVerificationStatus.Deactivated) adminProfile.TotalRejectedLawyers++;
                }

                var action = status == LawyerVerificationStatus.Active ? AdminLogAction.Verify : 
                             status == LawyerVerificationStatus.Suspended ? AdminLogAction.Suspend : AdminLogAction.Reject;
                await LogAdminActionAsync(adminId.Value, action, "Lawyer", userId);
            }
            await _context.SaveChangesAsync();
            return true;
        }

        private Guid? GetCurrentAdminId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier) ?? 
                        httpContext.User.FindFirst("id") ?? 
                        httpContext.User.FindFirst("sub");
            return claim != null && Guid.TryParse(claim.Value, out var adminId) ? adminId : null;
        }

        private UserResponseDto MapUserToDto(User user) => new()
        {
            Id = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = Decrypt(user.Phone),
            NationalId = Decrypt(user.NationalId),
            Gender = "ذكر",
            Nationality = user.Nationality ?? "مصري",
            Role = user.Role,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLogin,
            City = user.UserProfile?.City?.Name,
            GovernorateId = user.UserProfile?.City?.GovernorateId,
            GovernorateName = user.UserProfile?.City?.Governorate?.Name,
            Address = user.UserProfile?.Address
        };

        private AdminLogDto MapLogToDto(AdminLog log) => new() 
        { 
            Id = log.Id, 
            AdminName = log.Admin?.FullName ?? "غير معروف", 
            Action = log.Action, 
            TargetType = log.TargetType, 
            TargetId = log.TargetId, 
            Timestamp = log.Timestamp 
        };

        private LawyerResponseDto MapLawyerToDto(User user)
        {
            var lawyer = user.LawyerProfile!;
            var avgRating = lawyer.Reviews?.Any() == true ? lawyer.Reviews.Average(r => r.Rating) : 0;
            return new LawyerResponseDto
            {
                Id = lawyer.Id,
                UserId = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = Decrypt(user.Phone) ?? "",
                ProfilePicture = user.ProfilePicture ?? "",
                LicenseNumber = Decrypt(lawyer.LicenseNumber) ?? "",
                BarAssociation = lawyer.BarAssociation ?? "",
                YearsOfExperience = lawyer.YearsOfExperience ?? 0,
                VerificationStatus = lawyer.VerificationStatus.ToString(),
                IsActive = user.IsActive,
                VerifiedAt = lawyer.VerifiedAt,
                RejectionReason = lawyer.RejectionReason,
                Rating = (float)avgRating,
                TotalReviews = lawyer.Reviews?.Count ?? 0,
                GovernorateId = lawyer.GovernorateId,
                GovernorateName = lawyer.Governorate?.Name,
                City = lawyer.City?.Name,
                OfficeAddress = lawyer.OfficeAddress,
                Specialties = lawyer.Specialties?.Select(s => new LawyerProfileSpecialtyDto 
                { 
                    Id = s.SpecialtyId, 
                    Name = s.Specialty?.NameAr ?? "", 
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

        private string? Decrypt(string? encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return null;
            try { return _encryption.Decrypt(encrypted); }
            catch { return encrypted; }
        }
    }
}