// LegalMateAI.BLL/Services/Service/AdminService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using System.Text;

namespace LegalMateAI.BLL.Services.Service
{
    public class AdminService : IAdminService
    {
        private readonly LegalMateDbContext _context;

        public AdminService(LegalMateDbContext context)
        {
            _context = context;
        }

        // Dashboard
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

        // إدارة المحامين
        public async Task<List<LawyerResponseDto>> GetAllLawyersAsync(LawyerFilterDto? filter = null)
        {
            var query = _context.Users
                .Include(u => u.LawyerProfile)
                .Where(u => u.Role == UserRole.Lawyer && u.LawyerProfile != null);

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status) && 
                    Enum.TryParse<LawyerVerificationStatus>(filter.Status, true, out var status))
                {
                    query = query.Where(u => u.LawyerProfile!.VerificationStatus == status);
                }

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
                {
                    query = query.Where(u => u.LawyerProfile!.GovernorateId == filter.GovernorateId);
                }
            }

            int page = Math.Max(1, filter?.Page ?? 1);
            int pageSize = Math.Max(1, Math.Min(100, filter?.PageSize ?? 20));

            var lawyers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return lawyers.Select(u => MapLawyerToDto(u)).ToList();
        }

        public async Task<LawyerResponseDto?> GetLawyerDetailsAsync(Guid lawyerId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                 .ThenInclude(lp => lp!.Specialties)  // ✅ استخدم Specialties بدلاً من LawyerSpecializations
                .ThenInclude(s => s.Specialty)
                .Include(u => u.LawyerProfile)
                .ThenInclude(lp => lp!.Certificates)
                .Include(u => u.LawyerProfile)
                .ThenInclude(lp => lp!.Reviews)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null) return null;

            return MapLawyerToDto(user);
        }

        public async Task<bool> VerifyLawyerAsync(Guid adminId, Guid lawyerId, bool isApproved, string? rejectionReason = null)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null) return false;

            user.LawyerProfile.VerificationStatus = isApproved 
                ? LawyerVerificationStatus.Active 
                : LawyerVerificationStatus.Deactivated;
            
            user.LawyerProfile.VerifiedAt = isApproved ? DateTime.UtcNow : null;
            user.LawyerProfile.RejectionReason = isApproved ? null : rejectionReason;
            user.IsActive = isApproved;

            await LogAdminActionAsync(adminId, 
                isApproved ? AdminLogAction.Verify : AdminLogAction.Reject, 
                "Lawyer", lawyerId);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SuspendLawyerAsync(Guid adminId, Guid lawyerId, string? reason = null)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null) return false;

            user.LawyerProfile.VerificationStatus = LawyerVerificationStatus.Suspended;
            user.LawyerProfile.RejectionReason = reason;
            user.IsActive = false;

            await LogAdminActionAsync(adminId, AdminLogAction.Suspend, "Lawyer", lawyerId);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ActivateLawyerAsync(Guid adminId, Guid lawyerId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null) return false;

            user.LawyerProfile.VerificationStatus = LawyerVerificationStatus.Active;
            user.LawyerProfile.RejectionReason = null;
            user.IsActive = true;

            await LogAdminActionAsync(adminId, AdminLogAction.Activate, "Lawyer", lawyerId);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteLawyerAsync(Guid adminId, Guid lawyerId)
        {
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && u.Role == UserRole.Lawyer);

            if (user == null) return false;

            if (user.LawyerProfile != null)
            {
                _context.LawyerProfiles.Remove(user.LawyerProfile);
            }

            _context.Users.Remove(user);

            await LogAdminActionAsync(adminId, AdminLogAction.Suspend, "Lawyer", lawyerId);
            await _context.SaveChangesAsync();

            return true;
        }

        // إدارة المستخدمين
        public async Task<List<UserResponseDto>> GetAllUsersAsync(UserFilterDto? filter = null)
        {
            var query = _context.Users
                .Where(u => u.Role == UserRole.User);

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
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null) return null;

            return MapUserToDto(user);
        }

        public async Task<bool> UpdateUserStatusAsync(Guid adminId, Guid userId, AccountStatus status, string? reason = null)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null) return false;

            user.Status = status;
            user.IsActive = status == AccountStatus.Active;

            await LogAdminActionAsync(adminId, AdminLogAction.UpdateProfile, "User", userId);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid adminId, Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.User);

            if (user == null) return false;

            _context.Users.Remove(user);

            await LogAdminActionAsync(adminId, AdminLogAction.Suspend, "User", userId);
            await _context.SaveChangesAsync();

            return true;
        }

        // إدارة السجلات
        public async Task<List<AdminLogDto>> GetAdminLogsAsync(LogFilterDto? filter = null)
        {
            var query = _context.AdminLogs
                .Include(l => l.Admin)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.AdminId) && Guid.TryParse(filter.AdminId, out var adminId))
                {
                    query = query.Where(l => l.AdminId == adminId);
                }

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
            var logs = await GetAdminLogsAsync(filter);
            
            var csv = new StringBuilder();
            csv.AppendLine("التاريخ,الإجراء,المسؤول,النوع,المعرف");
            
            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Timestamp},{log.ActionName},{log.AdminName},{log.TargetType},{log.TargetId}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        // إدارة النظام
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
            await LogAdminActionAsync(adminId, AdminLogAction.UpdateProfile, "System", null);
            return true;
        }

        // دوال مساعدة
        private async Task<List<PendingLawyerDto>> GetPendingLawyersAsync()
        {
            return await _context.Users
                .Include(u => u.LawyerProfile)
                .Where(u => u.Role == UserRole.Lawyer && 
                       u.LawyerProfile!.VerificationStatus == LawyerVerificationStatus.Pending)
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
        }

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

        private LawyerResponseDto MapLawyerToDto(User user)
        {
            var lawyer = user.LawyerProfile!;
            var avgRating = lawyer.Reviews?.Any() == true ? lawyer.Reviews.Average(r => r.Rating) : 0;

            return new LawyerResponseDto
            {
                Id = user.UserID,
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
            YearsOfExperience = s.YearsOfExperience  // ✅ استخدم YearsOfExperience
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