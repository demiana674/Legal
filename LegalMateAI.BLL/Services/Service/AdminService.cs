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
using System.Text;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Document = QuestPDF.Fluent.Document;

namespace LegalMateAI.BLL.Services.Service
{
    public class AdminService : IAdminService
    {
        private readonly LegalMateDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AdminService> _logger;
        private readonly IEncryptionService _encryption;

        static AdminService()
        {
            // إعداد ترخيص EPPlus للإصدارات الجديدة
            ExcelPackage.License.SetNonCommercialPersonal("LegalMateAI");
            // إعداد ترخيص QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public AdminService(
            LegalMateDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AdminService> logger,
            IEncryptionService encryption)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _encryption = encryption;
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
            var query = _context.Users
                .Include(u => u.UserProfile)
                    .ThenInclude(p => p!.City)
                        .ThenInclude(c => c!.Governorate)
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

            var result = users.Select(MapUserToDto).ToList();
            
            foreach (var user in result)
            {
                user.PhoneNumber = Decrypt(user.PhoneNumber) ?? "";
                user.AlternativePhone = Decrypt(user.AlternativePhone);
                user.NationalId = Decrypt(user.NationalId);
            }
            
            return result;
        }

        public async Task<UserResponseDto?> GetUserDetailsAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                    .ThenInclude(p => p!.City)
                        .ThenInclude(c => c!.Governorate)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.User);
            
            if (user == null) return null;
            
            var result = MapUserToDto(user);
            
            result.PhoneNumber = Decrypt(result.PhoneNumber) ?? "";
            result.AlternativePhone = Decrypt(result.AlternativePhone);
            result.NationalId = Decrypt(result.NationalId);
            
            return result;
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
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == UserRole.User);
            if (user == null) return false;

            _context.Users.Remove(user);
            await LogAdminActionAsync(adminId, AdminLogAction.Delete, "User", userId);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== Lawyer Management ====================
        public async Task<List<PendingLawyerDto>> GetPendingLawyersAsync()
        {
            _logger.LogInformation("Getting pending lawyers...");

            var lawyers = await _context.Users
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

            foreach (var lawyer in lawyers)
            {
                lawyer.Phone = Decrypt(lawyer.Phone) ?? "";
                lawyer.LicenseNumber = Decrypt(lawyer.LicenseNumber) ?? "";
            }

            return lawyers;
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
                         u.LawyerProfile.LicenseNumber.Contains(term)));
                }

                if (filter.GovernorateId.HasValue)
                    query = query.Where(u => u.LawyerProfile!.GovernorateId == filter.GovernorateId);

                if (filter.SpecializationId.HasValue)
                    query = query.Where(u => u.LawyerProfile!.Specialties
                        .Any(s => s.SpecialtyId == filter.SpecializationId.Value));

                if (!string.IsNullOrEmpty(filter.City))
                    query = query.Where(u => u.LawyerProfile!.City != null &&
                        u.LawyerProfile.City.Name.Contains(filter.City));
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
                    .ThenInclude(lp => lp!.Governorate)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.City)
                .FirstOrDefaultAsync(u => u.UserID == lawyerId && u.Role == UserRole.Lawyer);

            return user?.LawyerProfile == null ? null : MapLawyerToDto(user);
        }

        public async Task<LawyerResponseDto?> GetLawyerDetailsByIdAsync(Guid lawyerId)
        {
            return await GetLawyerDetailsAsync(lawyerId);
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

        // ==================== Unified Log Management ====================
        
        public async Task<UnifiedLogsResponseDto> GetAllLogsAsync(UnifiedLogFilterDto filter)
        {
            var query = _context.AdminLogs
                .Include(l => l.Admin)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(l => 
                    (l.Admin != null && l.Admin.FullName.ToLower().Contains(term)) ||
                    (l.Admin != null && l.Admin.Email.ToLower().Contains(term)) ||
                    l.Action.ToString().ToLower().Contains(term) ||
                    l.TargetType.ToLower().Contains(term));
            }

            if (filter.AdminId.HasValue)
                query = query.Where(l => l.AdminId == filter.AdminId.Value);

            if (filter.UserId.HasValue)
                query = query.Where(l => l.TargetId == filter.UserId.Value);

            if (!string.IsNullOrEmpty(filter.TargetType))
                query = query.Where(l => l.TargetType == filter.TargetType);

            if (filter.Role.HasValue)
            {
                var targetUserIdsByRole = await _context.Users
                    .Where(u => u.Role == filter.Role.Value)
                    .Select(u => u.UserID)
                    .ToListAsync();
                query = query.Where(l => targetUserIdsByRole.Contains(l.TargetId));
            }

            if (filter.Action.HasValue)
                query = query.Where(l => l.Action == filter.Action.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(l => l.Timestamp >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
            {
                var endDate = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(l => l.Timestamp < endDate);
            }

            var totalCount = await query.CountAsync();
            int page = Math.Max(1, filter.Page);
            int pageSize = Math.Max(1, Math.Min(500, filter.PageSize));

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var targetUserIdsList = logs
                .Where(l => l.TargetType == "User" || l.TargetType == "Lawyer")
                .Select(l => l.TargetId)
                .Distinct()
                .ToList();
            
            var usersDict = await _context.Users
                .Where(u => targetUserIdsList.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID, u => u);
            
            var targetAdminIdsList = logs
                .Where(l => l.TargetType == "Admin")
                .Select(l => l.TargetId)
                .Distinct()
                .ToList();
            
            var adminsDict = await _context.Admins
                .Where(a => targetAdminIdsList.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a);
            
            var result = new List<UnifiedLogDto>();
            
            foreach (var log in logs)
            {
                var unifiedLog = new UnifiedLogDto
                {
                    Id = log.Id,
                    Timestamp = log.Timestamp,
                    AdminId = log.AdminId,
                    AdminName = log.Admin?.FullName ?? "غير معروف",
                    AdminEmail = log.Admin?.Email ?? "",
                    Action = log.Action,
                    TargetType = log.TargetType,
                    TargetId = log.TargetId
                };

                if (log.TargetId != Guid.Empty && (log.TargetType == "User" || log.TargetType == "Lawyer"))
                {
                    if (usersDict.TryGetValue(log.TargetId, out var targetUser))
                    {
                        unifiedLog.TargetName = targetUser.FullName;
                        unifiedLog.TargetEmail = targetUser.Email;
                        unifiedLog.TargetRole = targetUser.Role;
                    }
                }
                
                if (log.TargetId != Guid.Empty && log.TargetType == "Admin")
                {
                    if (adminsDict.TryGetValue(log.TargetId, out var targetAdmin))
                    {
                        unifiedLog.TargetName = targetAdmin.FullName;
                        unifiedLog.TargetEmail = targetAdmin.Email;
                    }
                }

                result.Add(unifiedLog);
            }

            return new UnifiedLogsResponseDto
            {
                Logs = result,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
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
                ActionsByType = allLogs
                    .GroupBy(l => l.Action.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        // ==================== Export Methods ====================
        
        public async Task<byte[]> ExportLogsToCsvAsync(UnifiedLogFilterDto filter)
        {
            var logsResponse = await GetAllLogsAsync(filter);
            var logs = logsResponse.Logs;
            
            var csv = new StringBuilder();
            csv.AppendLine("التاريخ,الأدمن,الإجراء,نوع المستهدف,المستهدف,البريد الإلكتروني");
            
            foreach (var log in logs)
            {
                csv.AppendLine($"\"{log.TimestampFormatted}\",\"{log.AdminName}\",\"{log.ActionDescription}\",\"{log.TargetTypeAr}\",\"{log.TargetName}\",\"{log.TargetEmail}\"");
            }
            
            var utf8WithBom = new UTF8Encoding(true);
            return utf8WithBom.GetBytes(csv.ToString());
        }

        public async Task<byte[]> ExportLogsToExcelAsync(UnifiedLogFilterDto filter)
        {
            var logsResponse = await GetAllLogsAsync(filter);
            var logs = logsResponse.Logs;
            
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("السجلات");
                
                worksheet.View.RightToLeft = true;
                
                using (var range = worksheet.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 12;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }
                
                worksheet.Cells[1, 1].Value = "التاريخ";
                worksheet.Cells[1, 2].Value = "الأدمن";
                worksheet.Cells[1, 3].Value = "الإجراء";
                worksheet.Cells[1, 4].Value = "نوع المستهدف";
                worksheet.Cells[1, 5].Value = "المستهدف";
                
                int row = 2;
                foreach (var log in logs)
                {
                    worksheet.Cells[row, 1].Value = log.TimestampFormatted;
                    worksheet.Cells[row, 2].Value = log.AdminName;
                    worksheet.Cells[row, 3].Value = log.ActionDescription;
                    worksheet.Cells[row, 4].Value = log.TargetTypeAr;
                    worksheet.Cells[row, 5].Value = log.TargetName;
                    row++;
                }
                
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                
                return package.GetAsByteArray();
            }
        }

        public async Task<byte[]> ExportLogsToPdfAsync(UnifiedLogFilterDto filter)
        {
            var logsResponse = await GetAllLogsAsync(filter);
            var logs = logsResponse.Logs;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));
                    
                    page.Header()
                        .Text("تقرير سجلات النظام")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium).AlignCenter();
                    
                    page.Header()
                        .PaddingTop(10)
                        .Text($"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1)
                        .AlignRight();
                    
                    page.Content()
                        .PaddingVertical(10)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });
                            
                            table.Header(header =>
                            {
                                header.Cell().Text("التاريخ").Bold();
                                header.Cell().Text("الأدمن").Bold();
                                header.Cell().Text("الإجراء").Bold();
                                header.Cell().Text("نوع المستهدف").Bold();
                                header.Cell().Text("المستهدف").Bold();
                            });
                            
                            foreach (var log in logs)
                            {
                                table.Cell().Text(log.TimestampFormatted);
                                table.Cell().Text(log.AdminName);
                                table.Cell().Text(log.ActionDescription);
                                table.Cell().Text(log.TargetTypeAr);
                                table.Cell().Text(log.TargetName ?? "-");
                            }
                        });
                    
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("الصفحة ");
                            text.CurrentPageNumber();
                            text.Span(" من ");
                            text.TotalPages();
                        });
                });
            });
            
            return document.GeneratePdf();
        }

        public async Task<byte[]> ExportLogsAsync(UnifiedLogFilterDto filter, string format = "csv")
        {
            return format.ToLower() switch
            {
                "csv" => await ExportLogsToCsvAsync(filter),
                "xlsx" => await ExportLogsToExcelAsync(filter),
                "pdf" => await ExportLogsToPdfAsync(filter),
                _ => await ExportLogsToCsvAsync(filter)
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
            };
        }

        public async Task<bool> ClearCacheAsync(Guid adminId)
        {
            await LogAdminActionAsync(adminId, AdminLogAction.ClearCache, "System", Guid.Empty);
            return true;
        }

        // ==================== Admin Details ====================
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
                Governorate = profile?.Governorate?.Name ?? "",
                City = profile?.City?.Name ?? "",
                Address = profile?.Address,
                CreatedAt = admin.CreatedAt,
                LastLoginAt = admin.LastLoginAt,
                TotalVerifiedLawyers = profile?.TotalVerifiedLawyers ?? 0,
                TotalRejectedLawyers = profile?.TotalRejectedLawyers ?? 0
            };
        }

        public async Task<AdminProfileDto?> GetAdminDetailsByIdAsync(Guid adminId)
        {
            return await GetAdminDetailsAsync(adminId);
        }

        // ==================== Entity Details ====================
        public async Task<object?> GetEntityDetailsAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Role == UserRole.Lawyer)
                    return await GetLawyerDetailsByIdAsync(id);
                else if (user.Role == UserRole.User)
                    return await GetUserDetailsAsync(id);
            }
            return await GetAdminDetailsAsync(id);
        }

        // ==================== Private Helpers ====================
        private async Task<List<AdminLogDto>> GetRecentActivityAsync(int count)
        {
            var logs = await _context.AdminLogs
                .Include(l => l.Admin)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();

            return logs.Select(l => new AdminLogDto
            {
                Id = l.Id,
                AdminName = l.Admin?.FullName ?? "غير معروف",
                Action = l.Action,
                TargetType = l.TargetType,
                TargetId = l.TargetId,
                Timestamp = l.Timestamp
            }).ToList();
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

        private async Task<bool> UpdateLawyerStatusAsync(Guid userId, LawyerVerificationStatus status, string? notes = null)
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

        private Guid? GetCurrentAdminId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirst("id")
                ?? httpContext.User.FindFirst("sub");

            return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var adminId) ? adminId : null;
        }

        private string? Decrypt(string? encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return null;
            try { return _encryption.Decrypt(encrypted); }
            catch { return encrypted; }
        }

        private UserResponseDto MapUserToDto(User user) => new()
        {
            Id = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.Phone ?? "",
            AlternativePhone = string.IsNullOrWhiteSpace(user.UserProfile?.AlternativePhone) ? null : user.UserProfile.AlternativePhone,
            NationalId = user.NationalId,
            Nationality = string.IsNullOrWhiteSpace(user.UserProfile?.Nationality) ? null : user.UserProfile.Nationality,
            Address = user.UserProfile?.Address,
            City = user.UserProfile?.City?.Name,
            GovernorateId = user.UserProfile?.City?.GovernorateId,
            GovernorateName = user.UserProfile?.City?.Governorate?.Name,
            Role = user.Role,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLogin,
            DocumentsCount = 0,
            ContractsCount = 0,
            AppointmentsCount = 0
        };

        private LawyerResponseDto MapLawyerToDto(User user)
        {
            var lawyer = user.LawyerProfile!;
            var avgRating = lawyer.Reviews?.Any() == true
                ? lawyer.Reviews.Average(r => r.Rating) : 0;

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
    }
}