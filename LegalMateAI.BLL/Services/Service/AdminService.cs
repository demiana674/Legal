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
using LegalMateAI.Infrastructure.Services.IService;

namespace LegalMateAI.BLL.Services.Service
{
    public class AdminService : IAdminService
    {
        private readonly LegalMateDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AdminService> _logger;
        private readonly IEncryptionService _encryptionService;

        public AdminService(
            LegalMateDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IEncryptionService encryptionService,
            ILogger<AdminService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        // ==================== Dashboard ====================
        public async Task<AdminDashboardDto> GetDashboardStatsAsync(Guid adminId)
        {
            var admin = await _context.Admins.FindAsync(adminId);
            var today = DateTime.UtcNow.Date;

            var recentActivity = await GetRecentActivityAsync(10, excludeActorId: adminId);

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
                RecentActivity = recentActivity
            };
        }

        // ==================== User Management ====================
        public async Task<List<UserResponseDto>> GetAllUsersAsync(UserFilterDto? filter = null)
        {
            var query = _context.Users
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.Governorate)
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.City)
                .Where(u => u.Role == UserRole.User)
                .AsQueryable();

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

            var result = new List<UserResponseDto>();
            foreach (var user in users)
            {
                result.Add(await MapUserToDtoAsync(user));
            }
            return result;
        }

        public async Task<UserResponseDto?> GetUserDetailsAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.Governorate)
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.City)
                .FirstOrDefaultAsync(u => u.UserID == userId);
                
            return user == null ? null : await MapUserToDtoAsync(user);
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
                    Phone = _encryptionService.Decrypt(u.Phone ?? ""),
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
                    .ThenInclude(lp => lp!.Certificates)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Governorate)
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.Governorate)
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.City)
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
                        (u.LawyerProfile!.LicenseNumber != null && u.LawyerProfile.LicenseNumber.ToLower().Contains(term)));
                }

                if (filter.GovernorateId.HasValue)
                    query = query.Where(u => u.LawyerProfile!.GovernorateId == filter.GovernorateId);

                if (filter.SpecializationId.HasValue)
                    query = query.Where(u => u.LawyerProfile!.Specialties.Any(s => s.SpecialtyId == filter.SpecializationId.Value));

                if (!string.IsNullOrEmpty(filter.City))
                    query = query.Where(u => u.LawyerProfile!.City != null && u.LawyerProfile.City.Name.Contains(filter.City));
            }

            int page = Math.Max(1, filter?.Page ?? 1);
            int pageSize = Math.Max(1, Math.Min(100, filter?.PageSize ?? 20));

            var lawyers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<LawyerResponseDto>();
            foreach (var lawyer in lawyers)
            {
                result.Add(await MapLawyerToDtoAsync(lawyer));
            }
            return result;
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
                    .ThenInclude(lp => lp!.City)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Governorate)
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.Governorate)
                .Include(u => u.UserProfile)
                    .ThenInclude(up => up!.City)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && u.Role == UserRole.Lawyer);

            return user?.LawyerProfile == null ? null : await MapLawyerToDtoAsync(user);
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
                case LawyerVerificationStatus.Pending:
                    user.IsActive = false;
                    user.Status = AccountStatus.Pending;
                    lawyerProfile.VerifiedAt = null;
                    break;
                case LawyerVerificationStatus.Deactivated:
                default:
                    user.IsActive = false;
                    user.Status = AccountStatus.Deactivated;
                    lawyerProfile.RejectionReason = notes;
                    break;
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue && status != LawyerVerificationStatus.Deactivated)
            {
                var action = status switch
                {
                    LawyerVerificationStatus.Active => AdminLogAction.Verify,
                    LawyerVerificationStatus.Suspended => AdminLogAction.Suspend,
                    _ => AdminLogAction.UpdateProfile
                };

                await LogActionAsync(currentUserId.Value, action, user.Role.ToString() ?? "Unknown", userId);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveLawyerAsync(Guid userId)
        {
            return await UpdateLawyerStatusAsync(userId, LawyerVerificationStatus.Active);
        }

        /// <summary>
        /// رفض المحامي - حذف نهائي من النظام مع تسجيل العملية في اللوجات
        /// </summary>
        public async Task<bool> RejectLawyerAsync(Guid userId, string reason)
        {
            _logger.LogInformation($"RejectLawyer - حذف نهائي للمحامي: {userId}, السبب: {reason}");

            // جلب المحامي مع جميع البيانات المرتبطة
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Certificates)
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user == null) return false;

            // ✅ تسجيل الرفض في اللوجات قبل الحذف (لكي يظهر للأدمن الآخرين)
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                await LogActionAsync(currentUserId.Value, AdminLogAction.Reject, "Lawyer", userId);
            }

            // 1. حذف التخصصات (LawyerProfileSpecialties)
            if (user.LawyerProfile?.Specialties != null && user.LawyerProfile.Specialties.Any())
            {
                _context.LawyerProfileSpecialties.RemoveRange(user.LawyerProfile.Specialties);
            }

            // 2. حذف الشهادات (Certificates)
            if (user.LawyerProfile?.Certificates != null && user.LawyerProfile.Certificates.Any())
            {
                _context.Certificates.RemoveRange(user.LawyerProfile.Certificates);
            }

            // 3. حذف الملف الشخصي للمحامي (LawyerProfile)
            if (user.LawyerProfile != null)
            {
                _context.LawyerProfiles.Remove(user.LawyerProfile);
            }

            // 4. حذف الـ UserProfile إن وجد
            if (user.UserProfile != null)
            {
                _context.UserProfiles.Remove(user.UserProfile);
            }

            // 5. حذف اللوجات التي يكون فيها هذا المحامي هو الفاعل (Actor)
            var logsWhereActorIsUser = _context.AdminLogs.Where(l => l.ActorId == userId);
            if (logsWhereActorIsUser.Any())
            {
                _context.AdminLogs.RemoveRange(logsWhereActorIsUser);
            }

            // 6. حذف المستخدم نفسه
            _context.Users.Remove(user);

            // حفظ جميع التغييرات
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ تم حذف المحامي {user.Email} نهائياً من النظام. السبب: {reason}");
            return true;
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
            var currentAdminId = GetCurrentUserId();
            var query = _context.AdminLogs.AsQueryable();

            // إخفاء نشاط الأدمن الحالي
            if (currentAdminId.HasValue)
            {
                query = query.Where(l => l.ActorId != currentAdminId.Value);
            }

            if (userId.HasValue && userId.Value != Guid.Empty)
                query = query.Where(l => l.ActorId == userId.Value || l.TargetId == userId.Value);

            if (action.HasValue)
                query = query.Where(l => l.Action == action.Value);

            if (!string.IsNullOrWhiteSpace(targetType))
                query = query.Where(l => l.TargetType == targetType);

            if (fromDate.HasValue)
                query = query.Where(l => l.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);
                query = query.Where(l => l.Timestamp < endDate);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(l =>
                    l.ActorName.ToLower().Contains(term) ||
                    l.ActorRole.ToLower().Contains(term) ||
                    l.Action.ToString().ToLower().Contains(term) ||
                    l.TargetType.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();

            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(500, pageSize));

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = logs.Select(l => MapLogToDto(l)).ToList();

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
                "pdf" => throw new NotImplementedException("PdfGenerationService needs to be added"),
                "excel" => throw new NotImplementedException("Excel export needs to be implemented"),
                _ => throw new NotImplementedException("Export service needs to be added")
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
        private async Task<List<AdminLogDto>> GetRecentActivityAsync(int count, Guid? excludeActorId = null)
        {
            var query = _context.AdminLogs.AsQueryable();

            if (excludeActorId.HasValue)
                query = query.Where(l => l.ActorId != excludeActorId.Value);

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();

            return logs.Select(l => MapLogToDto(l)).ToList();
        }

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

        // ==================== Mapping Methods with Full Data ====================
        
        private async Task<UserResponseDto> MapUserToDtoAsync(User user)
        {
            // حساب الإحصائيات
            var documentsCount = await _context.Documents.CountAsync(d => d.UserId == user.UserID);
            var contractsCount = await _context.Contracts.CountAsync(c => c.UserId == user.UserID);
            var appointmentsCount = await _context.Appointments.CountAsync(a => a.UserID == user.UserID);

            return new UserResponseDto
            {
                Id = user.UserID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = _encryptionService.Decrypt(user.Phone ?? ""),
                ProfilePicture = user.ProfilePicture,
                AlternativePhone = user.UserProfile != null ? _encryptionService.Decrypt(user.UserProfile.AlternativePhone ?? "") : null,
                NationalId = _encryptionService.Decrypt(user.NationalId ?? ""),
                GovernorateName = user.UserProfile?.Governorate?.Name,
                CityName = user.UserProfile?.City?.Name,
                Address = user.UserProfile?.Address,
                Nationality = user.UserProfile?.Nationality,
                Role = user.Role,
                Status = user.Status,
                EmailVerified = user.EmailVerified,
                CreatedAt = user.CreatedAt,
                DateOfBirth = user.UserProfile?.DateOfBirth?.ToString("yyyy-MM-dd"),
                LastLoginAt = user.LastLogin,
                DocumentsCount = documentsCount,
                ContractsCount = contractsCount,
                AppointmentsCount = appointmentsCount
            };
        }

        private async Task<LawyerResponseDto> MapLawyerToDtoAsync(User user)
        {
            var lawyer = user.LawyerProfile!;
            
       // حساب متوسط التقييم من LawyerReview
   double avgRating = await _context.LawyerReviews
    .Where(r => r.LawyerId == lawyer.Id)
    .AverageAsync(r => (double?)r.Rating) ?? 0;

            return new LawyerResponseDto
            {
                Id = lawyer.Id,
                UserId = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = _encryptionService.Decrypt(user.Phone ?? ""),
                AlternativePhone = user.UserProfile != null ? _encryptionService.Decrypt(user.UserProfile.AlternativePhone ?? "") : null,
                NationalId = _encryptionService.Decrypt(user.NationalId ?? ""),
                Nationality = user.Nationality ?? "",
                ProfilePicture = user.ProfilePicture ?? "",
                LicenseNumber = lawyer.LicenseNumber ?? "",
                BarAssociation = lawyer.BarAssociation ?? "",
                YearsOfExperience = lawyer.YearsOfExperience ?? 0,
                VerificationStatus = lawyer.VerificationStatus.ToString(),
                IsActive = user.IsActive,
                VerifiedAt = lawyer.VerifiedAt,
                RejectionReason = lawyer.RejectionReason,
                Rating = (float)avgRating,
                TotalReviews = 0, // يمكن حسابه من LawyerProfile.ReviewsCount
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

        private AdminLogDto MapLogToDto(AdminLog log)
        {
            object? targetDetails = null;

            string actorName = log.ActorName ?? "";

            if (string.IsNullOrWhiteSpace(actorName))
            {
                var adminActor = _context.Admins.FirstOrDefault(a => a.Id == log.ActorId);
                if (adminActor != null)
                {
                    actorName = adminActor.FullName;
                }
                else
                {
                    var userActor = _context.Users.FirstOrDefault(u => u.UserID == log.ActorId);
                    if (userActor != null)
                        actorName = userActor.FullName;
                }
            }

            if (log.TargetType == "User" || log.TargetType == "Lawyer")
            {
                var user = _context.Users.FirstOrDefault(u => u.UserID == log.TargetId);
                if (user != null)
                {
                    targetDetails = new
                    {
                        Id = user.UserID,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = _encryptionService.Decrypt(user.Phone ?? ""),
                        NationalId = _encryptionService.Decrypt(user.NationalId ?? ""),
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
                var admin = _context.Admins.FirstOrDefault(a => a.Id == log.TargetId);
                if (admin != null)
                {
                    targetDetails = new
                    {
                        Id = admin.Id,
                        FullName = admin.FullName,
                        Phone = _encryptionService.Decrypt(admin.PhoneNumber ?? ""),
                        Email = admin.Email
                    };
                }
            }

            return new AdminLogDto
            {
                Id = log.Id,
                Name = string.IsNullOrWhiteSpace(actorName) ? null : actorName,
                ActorId = log.ActorId == Guid.Empty ? null : log.ActorId,
                ActorRole = string.IsNullOrWhiteSpace(log.ActorRole) ? null : log.ActorRole,
                Action = log.Action,
                TargetType = log.TargetType,
                TargetId = log.TargetId,
                TargetDetails = targetDetails,
                Timestamp = log.Timestamp
            };
        }
    }
}