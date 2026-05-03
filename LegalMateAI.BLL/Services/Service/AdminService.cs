// LegalMateAI.BLL/Services/Service/AdminService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
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

        // ==================== Dashboard ====================
        public async Task<AdminDashboardDto> GetDashboardStatsAsync(Guid adminId)
        {
            var admin = await _context.Admins.FindAsync(adminId);
            var today = DateTime.UtcNow.Date;

            return new AdminDashboardDto
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
        }

        // ==================== User Management ====================
        public async Task<List<UserResponseDto>> GetAllUsersAsync(UserFilterDto? filter = null)
        {
            var query = _context.Users.Where(u => u.Role == UserRole.User).AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status) &&
                    Enum.TryParse<AccountStatus>(filter.Status, true, out var status))
                    query = query.Where(u => u.Status == status);

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

            return users.Select(MapUserToDto).ToList();
        }

        public async Task<UserResponseDto?> GetUserDetailsAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            return user == null ? null : MapUserToDto(user);
        }

        public async Task<bool> UpdateUserStatusAsync(Guid adminId, Guid userId, AccountStatus status, string? reason = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return false;

            user.Status = status;
            user.IsActive = status == AccountStatus.Active;

            await LogActionAsync(adminId, AdminLogAction.UpdateProfile, user.Role.ToString() ?? "Unknown", userId);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid adminId, Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.User);
            if (user == null) return false;

            _context.Users.Remove(user);
            await LogActionAsync(adminId, AdminLogAction.Delete, user.Role.ToString() ?? "Unknown", userId);
            await _context.SaveChangesAsync();
            return true;
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

            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var action = status switch
                {
                    LawyerVerificationStatus.Active => AdminLogAction.Verify,
                    LawyerVerificationStatus.Suspended => AdminLogAction.Suspend,
                    LawyerVerificationStatus.Deactivated => AdminLogAction.Reject,
                    _ => AdminLogAction.UpdateProfile
                };

                await LogActionAsync(currentUserId.Value, action, user.Role.ToString() ?? "Unknown", userId);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== Lawyer Management ====================
        public async Task<List<PendingLawyerDto>> GetPendingLawyersAsync()
        {
            _logger.LogInformation("Getting pending lawyers...");

            return await _context.Users
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
        }

        public async Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null)
        {
            var query = _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(s => s.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.City)
                .Where(u => u.Role == UserRole.Lawyer && u.LawyerProfile != null)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status) &&
                    Enum.TryParse<LawyerVerificationStatus>(filter.Status, true, out var status))
                    query = query.Where(u => u.LawyerProfile!.VerificationStatus == status);

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
                    query = query.Where(u => u.LawyerProfile!.GovernorateId == filter.GovernorateId);

                if (filter.SpecializationId.HasValue)
                    query = query.Where(u => u.LawyerProfile!.Specialties
                        .Any(s => s.SpecialtyId == filter.SpecializationId.Value));

                if (!string.IsNullOrEmpty(filter.City))
                {
                    query = query.Where(u => u.LawyerProfile!.City != null && 
                        u.LawyerProfile.City.Name.Contains(filter.City));
                }
            }

            int page = Math.Max(1, filter?.Page ?? 1);
            int pageSize = Math.Max(1, Math.Min(100, filter?.PageSize ?? 20));

            var lawyers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return lawyers.Select(MapLawyerToDto).ToList();
        }

        public async Task<LawyerResponseDto?> GetLawyerDetailsAsync(Guid lawyerId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(s => s.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Certificates)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.City)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && u.Role == UserRole.Lawyer);

            return user?.LawyerProfile == null ? null : MapLawyerToDto(user);
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

            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
                await LogActionAsync(currentUserId.Value, AdminLogAction.Delete, user.Role.ToString() ?? "Unknown", userId);

            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== Log Management ====================

        public async Task<PaginatedLogsDto<AdminLogDto>> GetAllLogsAsync(LogFilterDto? filter = null)
        {
            filter ??= new LogFilterDto();

            return await GetAllLogsInternalAsync(
                filter.UserId,
                filter.Action,
                filter.TargetType,
                filter.FromDate,
                filter.ToDate,
                filter.SearchTerm,
                filter.Page,
                filter.PageSize
            );
            
            }
        

       private async Task<PaginatedLogsDto<AdminLogDto>> GetAllLogsInternalAsync(
    Guid? userId,
    AdminLogAction? action,
    string? targetType,
    DateTime? fromDate,
    DateTime? toDate,
    string? searchTerm,
    int page,
    int pageSize)
{
    var query = _context.AdminLogs.AsQueryable();

    // فلترة باليوزر
    if (userId.HasValue && userId.Value != Guid.Empty)
    {
        query = query.Where(l =>
            l.ActorId == userId.Value ||
            l.TargetId == userId.Value);
    }

    // فلترة بالإجراء
    if (action.HasValue)
        query = query.Where(l => l.Action == action.Value);

    // فلترة بنوع المستهدف
    if (!string.IsNullOrWhiteSpace(targetType))
        query = query.Where(l => l.TargetType == targetType);

    // فلترة من تاريخ
    if (fromDate.HasValue)
        query = query.Where(l => l.Timestamp >= fromDate.Value);

    // فلترة إلى تاريخ
    if (toDate.HasValue)
    {
        var endDate = toDate.Value.Date.AddDays(1);
        query = query.Where(l => l.Timestamp < endDate);
    }

    // بحث
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        var term = searchTerm.Trim().ToLower();

        query = query.Where(l =>
            l.ActorName.ToLower().Contains(term) ||
            l.ActorRole.ToLower().Contains(term) ||
            l.Action.ToString().ToLower().Contains(term) ||
            l.TargetType.ToLower().Contains(term));
    } // ← دي كانت ناقصة

    // العدد الكلي
    var totalCount = await query.CountAsync();

    // Pagination
    page = Math.Max(1, page);
    pageSize = Math.Max(1, Math.Min(500, pageSize));

    var logs = await query
        .OrderByDescending(l => l.Timestamp)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var result = logs.Select(MapLogToDto).ToList();

    return new PaginatedLogsDto<AdminLogDto>
    {
        Items = result,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };
}
        public async Task<PaginatedLogsDto<AdminLogDto>> GetUserLogsAsync(Guid userId, LogFilterDto? filter = null)
        {
            filter ??= new LogFilterDto();
            filter.UserId = userId;
            return await GetAllLogsAsync(filter);
        }

        public async Task<byte[]> ExportLogsAsync(LogFilterDto? filter, string format = "csv")
        {
            var result = await GetAllLogsAsync(filter);
            var logs = result.Items;
            
            var adminName = "مدير النظام";
            if (filter?.UserId.HasValue == true)
            {
                var user = await _context.Users.FindAsync(filter.UserId.Value);
                if (user != null) adminName = user.FullName;
            }


return format.ToLower() switch
{
    "pdf"   => _pdfService.ExportAdminLogsToPdf(logs, adminName, filter?.FromDate, filter?.ToDate),
    "excel" => _pdfService.ExportAdminLogsToExcel(logs),
    _       => _pdfService.ExportAdminLogsToPdf(logs, adminName, filter?.FromDate, filter?.ToDate)
};

        }

        public async Task<byte[]> ExportLogsToPdfAsync(LogFilterDto? filter = null)
        {
            return await ExportLogsAsync(filter, "pdf");
        }

        public async Task<SystemLogsStatsDto> GetLogsStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var allLogs = await _context.AdminLogs.ToListAsync();

            return new SystemLogsStatsDto
            {
                TotalLogs = allLogs.Count,
                TodayLogs = allLogs.Count(l => l.Timestamp.Date == today),
                LoginAttempts = allLogs.Count(l => l.Action == AdminLogAction.Login),
                SuccessfulLogins = allLogs.Count(l => l.Action == AdminLogAction.Login),
                CasesCreated = await _context.Cases.CountAsync(c => c.CreatedAt.Date == today),
                AppointmentsBooked = await _context.Appointments.CountAsync(a => a.RequestedAt.Date == today),
                LawyersVerified = allLogs.Count(l => l.Action == AdminLogAction.Verify),
                LawyersRejected = allLogs.Count(l => l.Action == AdminLogAction.Reject),
                UsersRegistered = await _context.Users.CountAsync(u => u.CreatedAt.Date == today),
                AdminActions = allLogs.Count(l => l.Action != AdminLogAction.Login),
                LastActivityAt = allLogs.OrderByDescending(l => l.Timestamp).FirstOrDefault()?.Timestamp,
                ActionsByType = allLogs
                    .GroupBy(l => l.Action.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        // ==================== System Management ====================
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
                TotalStorageUsed = 0,
                LastBackupDate = DateTime.UtcNow.AddDays(-7),
                SystemUptime = 99.95
            };
        }

        public async Task<bool> ClearCacheAsync(Guid adminId)
        {
            await LogActionAsync(adminId, AdminLogAction.ClearCache, "System", Guid.Empty);
            return true;
        }

        // ==================== Private Helpers ====================

       private async Task<List<AdminLogDto>> GetRecentActivityAsync(int count)
{
    var logs = await _context.AdminLogs
        .OrderByDescending(l => l.Timestamp)
        .Take(count)
        .ToListAsync();

    return logs.Select(l => MapLogToDto(l)).ToList();
}
        /// <summary>
        /// تسجيل إجراء تلقائياً في اللوجز (لأي نوع مستخدم - أدمن/محامي/مستخدم عادي)
        /// </summary>
        private async Task LogActionAsync(Guid userId, AdminLogAction action, string targetType, Guid? targetId = null)
        {
            try
            {
                var log = new AdminLog
                {
                    Id = Guid.NewGuid(),
               ActorId = userId,
                    Action = action,
                    TargetType = targetType,
                    TargetId = targetId ?? Guid.Empty,
                    Timestamp = DateTime.UtcNow
                };
                _context.AdminLogs.Add(log);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ تم تسجيل الإجراء: {Action} - المستخدم: {UserId} - النوع: {TargetType}", 
                    action, userId, targetType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ في تسجيل اللوجز");
            }
        }

        /// <summary>
        /// الحصول على معرف المستخدم الحالي (أدمن/محامي/مستخدم)
        /// </summary>
        private Guid? GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirst("id")
                ?? httpContext.User.FindFirst("sub");

            if (userIdClaim == null) return null;

            return Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
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
                City = lawyer.City?.Name ?? "", 
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
    object? targetDetails = null;

    // ===== اسم الشخص اللي نفذ العملية (أي نوع مستخدم) =====
    string actorName = log.ActorName ?? "";

    if (string.IsNullOrWhiteSpace(actorName))
    {
        var adminActor = _context.Admins
            .FirstOrDefault(a => a.Id == log.ActorId);

        if (adminActor != null)
        {
            actorName = adminActor.FullName;
        }
        else
        {
            var userActor = _context.Users
                .FirstOrDefault(u => u.UserID == log.ActorId);

            if (userActor != null)
                actorName = userActor.FullName;
        }
    }

    // ===== تفاصيل المستهدف =====
    if (log.TargetType == "User" || log.TargetType == "Lawyer")
    {
        var user = _context.Users
            .FirstOrDefault(u => u.UserID == log.TargetId);

        if (user != null)
        {
            targetDetails = new
            {
                Id = user.UserID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                NationalId = user.NationalId,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            };
        }
    }
    else if (log.TargetType == "Admin")
    {
        var admin = _context.Admins
            .FirstOrDefault(a => a.Id == log.TargetId);

        if (admin != null)
        {
            targetDetails = new
            {
                Id = admin.Id,
                FullName = admin.FullName,
                Email = admin.Email
            };
        }
    }

    return new AdminLogDto
    {
        Id = log.Id,

        // الشخص اللي عمل العملية
    Name = string.IsNullOrWhiteSpace(actorName) ? null : actorName,
ActorId = log.ActorId == Guid.Empty ? null : log.ActorId,
ActorRole = string.IsNullOrWhiteSpace(log.ActorRole) ? null : log.ActorRole,

        Action = log.Action,

        // الشخص المستهدف
        TargetType = log.TargetType,
        TargetId = log.TargetId,
        TargetDetails = targetDetails,

        Timestamp = log.Timestamp
    };
}
    }
}