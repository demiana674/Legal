// LegalMateAI.BLL/Services/Service/AdminService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using System.Text;
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

        public AdminService(
            LegalMateDbContext context, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<AdminService> logger)
        {
            _context = context;
            _pdfService = new PdfGenerationService();
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // ========== Dashboard ==========
        public async Task<AdminDashboardDto> GetDashboardStatsAsync(Guid adminId)
        {
            var admin = await _context.Admins.FindAsync(adminId);
            var today = DateTime.UtcNow.Date;

            var stats = new AdminDashboardDto
            {
                TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User),
                TotalLawyers = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer),
                PendingVerifications = await _context.LawyerProfiles
                    .CountAsync(l => l.VerificationStatus == LawyerVerificationStatus.Pending),
                VerifiedToday = await _context.LawyerProfiles
                    .CountAsync(l => l.VerifiedAt.HasValue && l.VerifiedAt.Value.Date == today),
                AdminName = admin?.FullName ?? "",
                ProfilePicture = admin?.Profile?.ProfilePictureUrl,
                JobTitle = admin?.Profile?.JobTitle ?? "مدير النظام",
                PendingLawyers = await GetPendingLawyersAsync(),
                RecentActivity = await GetRecentActivityAsync(10)
            };

            return stats;
        }

        // ========== إدارة المستخدمين ==========
        public async Task<List<UserResponseDto>> GetAllUsersAsync(UserFilterDto? filter = null)
        {
            var query = _context.Users.Where(u => u.Role == UserRole.User);

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status) && 
                    Enum.TryParse<AccountStatus>(filter.Status, true, out var status))
                {
                    query = query.Where(u => u.Status == status);
                }

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var term = filter.SearchTerm.ToLower();
                    query = query.Where(u => 
                        u.FirstName.ToLower().Contains(term) ||
                        u.LastName.ToLower().Contains(term) ||
                        u.Email.ToLower().Contains(term));
                }

                if (filter.FromDate.HasValue)
                    query = query.Where(u => u.CreatedAt >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(u => u.CreatedAt <= filter.ToDate.Value);
            }

            int page = Math.Max(1, filter?.Page ?? 1);
            int pageSize = Math.Max(1, Math.Min(100, filter?.PageSize ?? 20));

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return users.Select(u => MapUserToDto(u)).ToList();
        }

        public async Task<UserResponseDto?> GetUserDetailsAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return null;
            return MapUserToDto(user);
        }

        public async Task<bool> UpdateUserStatusAsync(Guid adminId, Guid userId, AccountStatus status, string? reason = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return false;

            user.Status = status;
            user.IsActive = status == AccountStatus.Active;

            await LogAdminActionAsync(adminId, AdminLogAction.UpdateProfile, "User", userId);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid adminId, Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.User);
            if (user == null) return false;

            _context.Users.Remove(user);
            await LogAdminActionAsync(adminId, AdminLogAction.Delete, "User", userId);
            await _context.SaveChangesAsync();
            return true;
        }

        // ========== إدارة المحامين (من AdminLawyerService) ==========
        
        public async Task<List<PendingLawyerDto>> GetPendingLawyersAsync()
        {
            _logger.LogInformation("Getting pending lawyers...");
            
            var result = await _context.Users
                .Include(u => u.LawyerProfile)
                .Where(u => u.Role == UserRole.Lawyer && 
                       u.LawyerProfile != null &&
                       u.LawyerProfile.VerificationStatus == LawyerVerificationStatus.Pending)
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
                })
                .ToListAsync();
            
            _logger.LogInformation($"Found {result.Count} pending lawyers");
            return result;
        }

        public async Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null)
        {
            _logger.LogInformation("Getting all lawyers with filter: {@Filter}", filter);
            
            var query = _context.Users
                .Include(u => u.LawyerProfile)
                .ThenInclude(lp => lp!.Specialties)
                .ThenInclude(s => s.Specialty)
                .Where(u => u.Role == UserRole.Lawyer && u.LawyerProfile != null)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status))
                {
                    if (Enum.TryParse<LawyerVerificationStatus>(filter.Status, true, out var status))
                    {
                        query = query.Where(u => u.LawyerProfile!.VerificationStatus == status);
                    }
                }

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var term = filter.SearchTerm.ToLower();
                    query = query.Where(u => 
                        u.FirstName.ToLower().Contains(term) ||
                        u.LastName.ToLower().Contains(term) ||
                        u.Email.ToLower().Contains(term) ||
                        (u.LawyerProfile!.LicenseNumber != null && 
                         u.LawyerProfile.LicenseNumber.ToLower().Contains(term)));
                }

                if (filter.GovernorateId.HasValue)
                {
                    query = query.Where(u => u.LawyerProfile!.GovernorateId == filter.GovernorateId);
                }
                
                if (filter.SpecializationId.HasValue)
                {
                    query = query.Where(u => u.LawyerProfile!.Specialties
                        .Any(s => s.SpecialtyId == filter.SpecializationId.Value));
                }
                
                if (!string.IsNullOrEmpty(filter.City))
                {
                    query = query.Where(u => u.LawyerProfile!.City != null && 
                        u.LawyerProfile.City.Contains(filter.City));
                }
            }

            int page = Math.Max(1, filter?.Page ?? 1);
            int pageSize = Math.Max(1, Math.Min(100, filter?.PageSize ?? 20));

            var lawyers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation($"Found {lawyers.Count} lawyers");

            return lawyers.Select(u => MapLawyerToDto(u)).ToList();
        }

        public async Task<LawyerResponseDto?> GetLawyerDetailsAsync(Guid lawyerId)
        {
            _logger.LogInformation($"Getting lawyer details for: {lawyerId}");
            
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(s => s.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Certificates)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null) return null;

            return MapLawyerToDto(user);
        }

        public async Task<bool> UpdateLawyerStatusAsync(Guid userId, LawyerVerificationStatus status, string? notes = null)
        {
            _logger.LogInformation($"UpdateLawyerStatus - UserId: {userId}, Status: {status}");

            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null) return false;

            var lawyerProfile = user.LawyerProfile;
            lawyerProfile.VerificationStatus = status;
            
            switch (status)
            {
                case LawyerVerificationStatus.Active:
                    user.IsActive = true;
                    user.Status = AccountStatus.Active;
                    lawyerProfile.VerifiedAt = DateTime.UtcNow;
                    lawyerProfile.RejectionReason = null;
                    break;
                case LawyerVerificationStatus.Suspended:
                    user.IsActive = false;
                    user.Status = AccountStatus.Suspended;
                    lawyerProfile.RejectionReason = notes;
                    break;
                case LawyerVerificationStatus.Deactivated:
                    user.IsActive = false;
                    user.Status = AccountStatus.Deactivated;
                    lawyerProfile.RejectionReason = notes;
                    break;
                case LawyerVerificationStatus.Pending:
                    user.IsActive = false;
                    user.Status = AccountStatus.Pending;
                    lawyerProfile.VerifiedAt = null;
                    break;
            }

            var adminId = GetCurrentAdminId();
            if (adminId.HasValue)
            {
                var action = status switch
                {
                    LawyerVerificationStatus.Active => AdminLogAction.Verify,
                    LawyerVerificationStatus.Suspended => AdminLogAction.Suspend,
                    LawyerVerificationStatus.Deactivated => AdminLogAction.Reject,
                    _ => AdminLogAction.UpdateProfile
                };

                await LogAdminActionAsync(adminId.Value, action, "Lawyer", userId);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveLawyerAsync(Guid userId)
        {
            return await UpdateLawyerStatusAsync(userId, LawyerVerificationStatus.Active);
        }

        public async Task<bool> RejectLawyerAsync(Guid userId, string reason)
        {
            return await UpdateLawyerStatusAsync(userId, LawyerVerificationStatus.Deactivated, reason);
        }

        public async Task<bool> SuspendLawyerAsync(Guid userId, string? reason = null)
        {
            return await UpdateLawyerStatusAsync(userId, LawyerVerificationStatus.Suspended, reason);
        }

        public async Task<bool> ActivateLawyerAsync(Guid userId)
        {
            return await UpdateLawyerStatusAsync(userId, LawyerVerificationStatus.Active);
        }

        public async Task<bool> DeleteLawyerAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user == null) return false;

            if (user.LawyerProfile != null)
                _context.LawyerProfiles.Remove(user.LawyerProfile);

            _context.Users.Remove(user);
            
            var adminId = GetCurrentAdminId();
            if (adminId.HasValue)
                await LogAdminActionAsync(adminId.Value, AdminLogAction.Delete, "Lawyer", userId);
                
            await _context.SaveChangesAsync();
            return true;
        }

        // ========== دوال الموافقة القديمة (للتوافق) ==========
        public async Task<bool> VerifyLawyerAsync(Guid adminId, Guid lawyerId, bool isApproved, string? rejectionReason = null)
        {
            return isApproved 
                ? await ApproveLawyerAsync(lawyerId) 
                : await RejectLawyerAsync(lawyerId, rejectionReason ?? "غير محدد");
        }

        async Task<bool> IAdminService.SuspendLawyerAsync(Guid adminId, Guid lawyerId, string? reason)
        {
            return await SuspendLawyerAsync(lawyerId, reason);
        }

        async Task<bool> IAdminService.ActivateLawyerAsync(Guid adminId, Guid lawyerId)
        {
            return await ActivateLawyerAsync(lawyerId);
        }

        async Task<bool> IAdminService.DeleteLawyerAsync(Guid adminId, Guid lawyerId)
        {
            return await DeleteLawyerAsync(lawyerId);
        }

        // ========== إدارة السجلات ==========
        public async Task<List<AdminLogDto>> GetAdminLogsAsync(LogFilterDto? filter = null)
        {
            var query = _context.AdminLogs.Include(l => l.Admin).AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.AdminId) && Guid.TryParse(filter.AdminId, out var adminId))
                    query = query.Where(l => l.AdminId == adminId);

                if (filter.Action.HasValue)
                    query = query.Where(l => l.Action == filter.Action.Value);

                if (!string.IsNullOrEmpty(filter.TargetType))
                    query = query.Where(l => l.TargetType == filter.TargetType);

                if (filter.FromDate.HasValue)
                    query = query.Where(l => l.Timestamp >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(l => l.Timestamp <= filter.ToDate.Value);
            }

            int page = Math.Max(1, filter?.Page ?? 1);
            int pageSize = Math.Max(1, Math.Min(500, filter?.PageSize ?? 50));

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return logs.Select(l => MapLogToDto(l)).ToList();
        }

        public async Task<byte[]> ExportLogsAsync(LogFilterDto? filter = null)
        {
            return await ExportLogsAsync(filter, "csv");
        }

        public async Task<byte[]> ExportLogsAsync(LogFilterDto? filter, string format)
        {
            var logs = await GetAdminLogsAsync(filter);
            
            if (format.ToLower() == "pdf")
            {
                var adminName = "مدير النظام";
                if (!string.IsNullOrEmpty(filter?.AdminId) && Guid.TryParse(filter.AdminId, out var adminId))
                {
                    var admin = await _context.Admins.FindAsync(adminId);
                    if (admin != null) adminName = admin.FullName;
                }
                return _pdfService.ExportAdminLogsToPdf(logs, adminName, filter?.FromDate, filter?.ToDate);
            }
            else
            {
                return _pdfService.ExportAdminLogsToExcel(logs);
            }
        }

        public async Task<byte[]> ExportLogsToPdfAsync(LogFilterDto? filter = null)
        {
            var logs = await GetAdminLogsAsync(filter);
            var adminName = "مدير النظام";
            
            if (!string.IsNullOrEmpty(filter?.AdminId) && Guid.TryParse(filter.AdminId, out var adminId))
            {
                var admin = await _context.Admins.FindAsync(adminId);
                if (admin != null) adminName = admin.FullName;
            }
            
            return _pdfService.ExportAdminLogsToPdf(logs, adminName, filter?.FromDate, filter?.ToDate);
        }

        // ========== إدارة النظام ==========
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
                PendingVerifications = await _context.LawyerProfiles
                    .CountAsync(l => l.VerificationStatus == LawyerVerificationStatus.Pending),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive && u.Role == UserRole.User),
                ActiveLawyers = await _context.Users.CountAsync(u => u.IsActive && u.Role == UserRole.Lawyer),
                TotalStorageUsed = await GetTotalStorageUsedAsync(),
                LastBackupDate = DateTime.UtcNow.AddDays(-7),
                SystemUptime = 99.95
            };
        }

        public async Task<bool> ClearCacheAsync(Guid adminId)
        {
            await LogAdminActionAsync(adminId, AdminLogAction.ClearCache, "System", Guid.Empty);
            return true;
        }

        // ========== دوال مساعدة ==========
        private async Task<List<AdminLogDto>> GetRecentActivityAsync(int count)
        {
            var logs = await _context.AdminLogs
                .Include(l => l.Admin)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();

            return logs.Select(l => MapLogToDto(l)).ToList();
        }

        private async Task LogAdminActionAsync(Guid adminId, AdminLogAction action, string targetType, Guid? targetId)
        {
            var log = new AdminLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = action,
                TargetType = targetType,
                TargetId = targetId ?? Guid.Empty,
                Timestamp = DateTime.UtcNow
            };
            _context.AdminLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private async Task<long> GetTotalStorageUsedAsync()
        {
            var documents = await _context.Documents.ToListAsync();
            return documents.Sum(d => d.FileSize);
        }

        private Guid? GetCurrentAdminId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirst("id")
                ?? httpContext.User.FindFirst("sub");

            if (userIdClaim == null) return null;

            return Guid.TryParse(userIdClaim.Value, out var adminId) ? adminId : null;
        }

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
                PhoneNumber = user.Phone ?? "",
                ProfilePicture = user.ProfilePicture ?? "",
                LicenseNumber = lawyer.LicenseNumber ?? "",
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
                City = lawyer.City,
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

        private UserResponseDto MapUserToDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.UserID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.Phone ?? "",
                NationalId = user.NationalId,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLogin
            };
        }

        private AdminLogDto MapLogToDto(AdminLog log)
        {
            return new AdminLogDto
            {
                Id = log.Id,
                AdminName = log.Admin?.FullName ?? "غير معروف",
                Action = log.Action,
                TargetType = log.TargetType,
                TargetId = log.TargetId,
                Timestamp = log.Timestamp
            };
        }
    }
}