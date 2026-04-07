// LegalMateAI.BLL/Services/Service/AdminLawyerService.cs
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
    public class AdminLawyerService : IAdminLawyerService
    {
        private readonly LegalMateDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AdminLawyerService> _logger;

        public AdminLawyerService(
            LegalMateDbContext context, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<AdminLawyerService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

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
            }

            int page = Math.Max(1, filter?.Page ?? 1);
            int pageSize = Math.Max(1, Math.Min(100, filter?.PageSize ?? 20));

            var lawyers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation($"Found {lawyers.Count} lawyers");

            return lawyers.Select(u => new LawyerResponseDto
            {
                Id = u.LawyerProfile!.Id,
                UserId = u.UserID,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.Phone ?? "",
                ProfilePicture = u.ProfilePicture ?? "",
                LicenseNumber = u.LawyerProfile.LicenseNumber ?? "",
                BarAssociation = u.LawyerProfile.BarAssociation ?? "",
                YearsOfExperience = u.LawyerProfile.YearsOfExperience ?? 0,
                VerificationStatus = u.LawyerProfile.VerificationStatus.ToString(),
                IsActive = u.IsActive,
                RejectionReason = u.LawyerProfile.RejectionReason,
                VerifiedAt = u.LawyerProfile.VerifiedAt
            }).ToList();
        }

        public async Task<LawyerResponseDto?> GetLawyerByIdAsync(Guid userId)
        {
            _logger.LogInformation($"Getting lawyer by UserId: {userId}");
            
            var user = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

            if (user?.LawyerProfile == null)
            {
                _logger.LogWarning($"Lawyer not found with UserId: {userId}");
                return null;
            }

            return new LawyerResponseDto
            {
                Id = user.LawyerProfile.Id,
                UserId = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.Phone ?? "",
                ProfilePicture = user.ProfilePicture ?? "",
                LicenseNumber = user.LawyerProfile.LicenseNumber ?? "",
                BarAssociation = user.LawyerProfile.BarAssociation ?? "",
                YearsOfExperience = user.LawyerProfile.YearsOfExperience ?? 0,
                VerificationStatus = user.LawyerProfile.VerificationStatus.ToString(),
                IsActive = user.IsActive,
                RejectionReason = user.LawyerProfile.RejectionReason,
                VerifiedAt = user.LawyerProfile.VerifiedAt
            };
        }

        // ✅ تحديث حالة المحامي
        public async Task<bool> UpdateLawyerStatusAsync(Guid userId, LawyerVerificationStatus status, string? notes = null)
        {
            _logger.LogInformation("=== UPDATE LAWYER STATUS STARTED ===");
            _logger.LogInformation($"UserId: {userId}, Status: {status}, Notes: {notes}");

            try
            {
                // ✅ جلب المستخدم مع الـ LawyerProfile
                var user = await _context.Users
                    .Include(u => u.LawyerProfile)
                    .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

                if (user?.LawyerProfile == null)
                {
                    _logger.LogWarning($"Lawyer not found with UserId: {userId}");
                    return false;
                }

                var lawyerProfile = user.LawyerProfile;

                _logger.LogInformation($"Found lawyer: {user.FirstName} {user.LastName}");
                _logger.LogInformation($"Current VerificationStatus: {lawyerProfile.VerificationStatus}");
                _logger.LogInformation($"Current IsActive: {user.IsActive}");
                _logger.LogInformation($"Current User.Status: {user.Status}");

                // ✅ تحديث الحالة
                lawyerProfile.VerificationStatus = status;
                
                // ✅ تحديث الـ IsActive والحقول الأخرى بناءً على الحالة
                switch (status)
                {
                    case LawyerVerificationStatus.Active:
                        user.IsActive = true;
                        user.Status = AccountStatus.Active;
                        lawyerProfile.VerifiedAt = DateTime.UtcNow;
                        lawyerProfile.RejectionReason = null;
                        lawyerProfile.PracticeDegree = "مقبول";
                        break;
                        
                    case LawyerVerificationStatus.Locked:
                        user.IsActive = false;
                        user.Status = AccountStatus.Locked;
                        lawyerProfile.VerifiedAt = null;
                        lawyerProfile.RejectionReason = notes;
                        lawyerProfile.PracticeDegree = "قيد المراجعة";
                        break;
                        
                    case LawyerVerificationStatus.Suspended:
                        user.IsActive = false;
                        user.Status = AccountStatus.Suspended;
                        lawyerProfile.RejectionReason = notes;
                        lawyerProfile.PracticeDegree = "موقوف";
                        break;
                        
                    case LawyerVerificationStatus.Deactivated:
                        user.IsActive = false;
                        user.Status = AccountStatus.Deactivated;
                        lawyerProfile.RejectionReason = notes;
                        lawyerProfile.PracticeDegree = "مرفوض";
                        break;
                        
                    case LawyerVerificationStatus.Pending:
                        user.IsActive = false;
                        user.Status = AccountStatus.Pending;
                        lawyerProfile.VerifiedAt = null;
                        lawyerProfile.PracticeDegree = "قيد الانتظار";
                        break;
                }

                _logger.LogInformation($"Updated - IsActive: {user.IsActive}, User.Status: {user.Status}");
                _logger.LogInformation($"Updated - VerificationStatus: {lawyerProfile.VerificationStatus}");

                // تسجيل في AdminLog
                var adminId = GetCurrentAdminId();
                if (adminId.HasValue)
                {
                    var action = status switch
                    {
                        LawyerVerificationStatus.Active => AdminLogAction.Verify,
                        LawyerVerificationStatus.Suspended => AdminLogAction.Suspend,
                        LawyerVerificationStatus.Deactivated => AdminLogAction.Reject,
                        LawyerVerificationStatus.Locked => AdminLogAction.UpdateProfile,
                        _ => AdminLogAction.UpdateProfile
                    };

                    var adminLog = new AdminLog
                    {
                        Id = Guid.NewGuid(),
                        AdminId = adminId.Value,
                        Action = action,
                        TargetType = "Lawyer",
                        TargetId = user.UserID,
                        Timestamp = DateTime.UtcNow
                    };
                    _context.AdminLogs.Add(adminLog);
                    _logger.LogInformation($"AdminLog added with ID: {adminLog.Id}");
                }

                // ✅ Save Changes
                var savedRows = await _context.SaveChangesAsync();
                _logger.LogInformation($"SaveChangesAsync affected {savedRows} row(s)");

                if (savedRows > 0)
                {
                    _logger.LogInformation($"✅ Lawyer {userId} status updated successfully!");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"⚠️ No changes were saved for lawyer {userId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating lawyer status for {userId}");
                return false;
            }
            finally
            {
                _logger.LogInformation("=== UPDATE LAWYER STATUS ENDED ===");
            }
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
            _logger.LogInformation($"=== DELETE LAWYER STARTED ===");
            _logger.LogInformation($"UserId: {userId}");
            
            try
            {
                var user = await _context.Users
                    .Include(u => u.LawyerProfile)
                    .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.Lawyer);

                if (user == null)
                {
                    _logger.LogWarning($"Lawyer not found with UserId: {userId}");
                    return false;
                }

                if (user.LawyerProfile != null)
                {
                    _context.LawyerProfiles.Remove(user.LawyerProfile);
                    _logger.LogInformation($"Removed LawyerProfile for {userId}");
                }

                _context.Users.Remove(user);
                _logger.LogInformation($"Removed User {userId}");

                var savedRows = await _context.SaveChangesAsync();
                _logger.LogInformation($"SaveChangesAsync affected {savedRows} row(s)");
                
                return savedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting lawyer {userId}");
                return false;
            }
            finally
            {
                _logger.LogInformation("=== DELETE LAWYER ENDED ===");
            }
        }

        private Guid? GetCurrentAdminId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HttpContext is null");
                return null;
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim == null)
            {
                userIdClaim = httpContext.User.FindFirst("id");
            }
            
            if (userIdClaim == null)
            {
                userIdClaim = httpContext.User.FindFirst("sub");
            }
            
            if (userIdClaim == null)
            {
                _logger.LogWarning("UserId claim not found in HttpContext");
                return null;
            }

            if (Guid.TryParse(userIdClaim.Value, out var adminId))
            {
                _logger.LogDebug($"Found AdminId: {adminId}");
                return adminId;
            }

            _logger.LogWarning($"Failed to parse AdminId from: {userIdClaim.Value}");
            return null;
        }
    }
}