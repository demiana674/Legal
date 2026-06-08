using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace LegalMateAI.BLL.Services.Service
{
    public class CaseService : ICaseService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CaseService> _logger;

        public CaseService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            ILogger<CaseService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // ========== CRUD للقضايا ==========

        public async Task<CaseResponseDto?> CreateCaseAsync(Guid userId, CreateCaseDto request, bool isLawyer = false)
        {
            try
            {
                _logger.LogInformation($"🔵 CreateCaseAsync - userId: {userId}, isLawyer: {isLawyer}");
                
                Guid clientId;
                
                if (request.ClientId.HasValue && request.ClientId.Value != Guid.Empty)
                {
                    var existingClient = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserID == request.ClientId.Value && u.Role == UserRole.User);
                    
                    if (existingClient != null)
                    {
                        clientId = existingClient.UserID;
                    }
                    else
                    {
                        clientId = await CreateNewClientAsync(request);
                    }
                }
                else if (!string.IsNullOrEmpty(request.ClientEmail) || !string.IsNullOrEmpty(request.ClientPhone))
                {
                    var existingClient = await _context.Users
                        .FirstOrDefaultAsync(u => u.Role == UserRole.User && 
                            (u.Email == request.ClientEmail || u.Phone == request.ClientPhone));
                    
                    if (existingClient != null)
                    {
                        clientId = existingClient.UserID;
                    }
                    else
                    {
                        clientId = await CreateNewClientAsync(request);
                    }
                }
                else
                {
                    clientId = await CreateNewClientAsync(request);
                }

                Guid? lawyerProfileId = null;
                if (isLawyer)
                {
                    var lawyer = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId);
                    if (lawyer == null) return null;
                    lawyerProfileId = lawyer.Id;
                }

                var caseNumber = await GenerateCaseNumberAsync();

                var newCase = new Case
                {
                    Id = Guid.NewGuid(),
                    CaseNumber = caseNumber,
                    Title = request.Title,
                    Description = request.Description,
                    ClientId = clientId,
                    LawyerId = lawyerProfileId,
                    Court = request.Court,
                    NextHearingDate = request.NextHearingDate,
                    Status = request.Status,
                    Priority = request.Priority,
                    CaseType = request.CaseType,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Cases.Add(newCase);
                await _context.SaveChangesAsync();
                
                return await GetCaseByIdAsync(userId, newCase.Id, isLawyer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in CreateCaseAsync: {ex.Message}");
                return null;
            }
        }

        private async Task<Guid> CreateNewClientAsync(CreateCaseDto request)
        {
            var tempPassword = GenerateRandomPassword();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            var tempNationalId = GenerateTempNationalId();
            
            var email = request.ClientEmail ?? $"client_{Guid.NewGuid():N}@tempclient.com";
            
            var newClient = new User
            {
                UserID = Guid.NewGuid(),
                FirstName = request.ClientFirstName ?? "موكل",
                LastName = request.ClientLastName ?? "جديد",
                Email = email,
                PasswordHash = passwordHash,
                Phone = request.ClientPhone ?? "",
                NationalId = tempNationalId,
                Nationality = request.ClientNationality ?? "مصري",
                DateOfBirth = request.ClientDateOfBirth,
                Role = UserRole.User,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                JoinDate = DateTime.UtcNow,
                EmailVerified = false
            };
            
            _context.Users.Add(newClient);
            
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = newClient.UserID,
                FirstName = newClient.FirstName,
                LastName = newClient.LastName,
                Email = newClient.Email,
                PhoneNumber = newClient.Phone,
                NationalId = tempNationalId,
                Nationality = newClient.Nationality,
                DateOfBirth = request.ClientDateOfBirth,
                Governorate = request.ClientGovernorate,
                City = request.ClientCity,
                Address = request.ClientAddress,
                CreatedAt = DateTime.UtcNow,
                LastProfileUpdate = DateTime.UtcNow
            };
            
            _context.UserProfiles.Add(userProfile);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"✅ New client created: {newClient.Email}");
            
            return newClient.UserID;
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateTempNationalId()
        {
            var random = new Random();
            return $"TEMP{DateTime.Now.Ticks}{random.Next(1000, 9999)}";
        }

        private async Task<string> GenerateCaseNumberAsync()
        {
            var count = await _context.Cases.CountAsync() + 1;
            return $"CS-{DateTime.UtcNow.Year}-{count:D6}";
        }

        private async Task<LawyerProfile?> GetLawyerProfileByUserIdAsync(Guid userId)
        {
            return await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId);
        }

        public async Task<CaseResponseDto?> UpdateCaseAsync(Guid userId, Guid caseId, UpdateCaseDto request, bool isLawyer = false)
        {
            try
            {
                var existingCase = await _context.Cases
                    .Include(c => c.Client)
                    .Include(c => c.Lawyer)
                        .ThenInclude(l => l!.User)
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                if (existingCase == null) return null;
                
                if (isLawyer)
                {
                    var lawyer = await GetLawyerProfileByUserIdAsync(userId);
                    if (lawyer == null || existingCase.LawyerId != lawyer.Id) return null;
                }
                else if (!isLawyer && existingCase.ClientId != userId) return null;

                bool hasChanges = false;
                
                if (!string.IsNullOrEmpty(request.Title) && existingCase.Title != request.Title) 
                { 
                    existingCase.Title = request.Title; 
                    hasChanges = true; 
                }
                
                if (!string.IsNullOrEmpty(request.Description) && existingCase.Description != request.Description) 
                { 
                    existingCase.Description = request.Description; 
                    hasChanges = true; 
                }
                
                if (request.Status.HasValue && existingCase.Status != request.Status) 
                { 
                    existingCase.Status = request.Status.Value; 
                    if (request.Status.Value == CaseStatus.Completed || request.Status.Value == CaseStatus.Rejected) 
                        existingCase.ClosedAt = DateTime.UtcNow; 
                    hasChanges = true; 
                }
                
                if (request.Priority.HasValue && existingCase.Priority != request.Priority) 
                { 
                    existingCase.Priority = request.Priority.Value; 
                    hasChanges = true; 
                }

                if (hasChanges) 
                { 
                    existingCase.UpdatedAt = DateTime.UtcNow; 
                    await _context.SaveChangesAsync(); 
                }
                
                return await GetCaseByIdAsync(userId, caseId, isLawyer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in UpdateCaseAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteCaseAsync(Guid userId, Guid caseId, bool isLawyer = false)
        {
            try
            {
                var existingCase = await _context.Cases
                    .Include(c => c.Documents)
                    .Include(c => c.Notes)
                    .FirstOrDefaultAsync(c => c.Id == caseId);
                    
                if (existingCase == null) return false;
                
                if (isLawyer)
                {
                    var lawyer = await GetLawyerProfileByUserIdAsync(userId);
                    if (lawyer == null || existingCase.LawyerId != lawyer.Id) return false;
                }
                else if (!isLawyer && existingCase.ClientId != userId) return false;

                foreach (var doc in existingCase.Documents)
                {
                    var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), doc.FileUrl.TrimStart('/'));
                    if (File.Exists(filePath)) File.Delete(filePath);
                }

                _context.Cases.Remove(existingCase);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in DeleteCaseAsync: {ex.Message}");
                return false;
            }
        }

        // ========== جلب القضايا ==========

        public async Task<List<CaseResponseDto>> GetCasesAsync(CaseFilterDto filter)
        {
            try
            {
                var query = _context.Cases
                    .Include(c => c.Client)
                    .Include(c => c.Lawyer)
                        .ThenInclude(l => l!.User)
                    .Include(c => c.Documents)
                    .Include(c => c.Notes)
                    .AsQueryable();
                    
                if (filter.ClientId.HasValue) query = query.Where(c => c.ClientId == filter.ClientId.Value);
                if (filter.LawyerId.HasValue) query = query.Where(c => c.LawyerId == filter.LawyerId.Value);
                if (filter.Status.HasValue) query = query.Where(c => c.Status == filter.Status.Value);
                if (!string.IsNullOrEmpty(filter.CaseType)) query = query.Where(c => c.CaseType == filter.CaseType);
                
                if (!string.IsNullOrEmpty(filter.SearchTerm)) 
                { 
                    var t = filter.SearchTerm.ToLower(); 
                    query = query.Where(c => c.Title.ToLower().Contains(t) || 
                        (c.Description != null && c.Description.ToLower().Contains(t)) ||
                        (c.Client != null && c.Client.FullName.ToLower().Contains(t)) ||
                        (c.CaseNumber.ToLower().Contains(t)));
                }

                int page = Math.Max(1, filter.Page);
                int pageSize = Math.Max(1, Math.Min(100, filter.PageSize));
                
                var cases = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                    
                return cases.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in GetCasesAsync: {ex.Message}");
                return new List<CaseResponseDto>();
            }
        }

        public async Task<List<CaseResponseDto>> GetCasesForUserAsync(Guid userId, CaseFilterDto filter, bool isLawyer = false)
        {
            try
            {
                var query = _context.Cases
                    .Include(c => c.Client)
                    .Include(c => c.Lawyer)
                        .ThenInclude(l => l!.User)
                    .Include(c => c.Documents)
                    .Include(c => c.Notes)
                    .AsQueryable();
                
                if (isLawyer)
                {
                    var lawyer = await GetLawyerProfileByUserIdAsync(userId);
                    if (lawyer != null)
                    {
                        query = query.Where(c => c.LawyerId == lawyer.Id);
                    }
                    else
                    {
                        return new List<CaseResponseDto>();
                    }
                }
                else
                {
                    query = query.Where(c => c.ClientId == userId);
                }
                
                if (filter.Status.HasValue)
                {
                    query = query.Where(c => c.Status == filter.Status.Value);
                }
                
                if (!string.IsNullOrEmpty(filter.CaseType))
                {
                    query = query.Where(c => c.CaseType == filter.CaseType);
                }
                
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var term = filter.SearchTerm.ToLower();
                    query = query.Where(c => c.Title.ToLower().Contains(term) || 
                        (c.Description != null && c.Description.ToLower().Contains(term)) ||
                        (c.Client != null && c.Client.FullName.ToLower().Contains(term)) ||
                        (c.CaseNumber.ToLower().Contains(term)));
                }

                int page = Math.Max(1, filter.Page);
                int pageSize = Math.Max(1, Math.Min(100, filter.PageSize));
                
                var cases = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                    
                return cases.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in GetCasesForUserAsync: {ex.Message}");
                return new List<CaseResponseDto>();
            }
        }

        public async Task<CaseResponseDto?> GetCaseByIdAsync(Guid userId, Guid caseId, bool isLawyer = false)
        {
            try
            {
                var c = await _context.Cases
                    .Include(x => x.Client)
                    .Include(x => x.Lawyer)
                        .ThenInclude(l => l!.User)
                    .Include(x => x.Documents)
                    .Include(x => x.Notes)
                    .FirstOrDefaultAsync(x => x.Id == caseId);
                    
                if (c == null) return null;
                
                if (isLawyer)
                {
                    var lawyer = await GetLawyerProfileByUserIdAsync(userId);
                    if (lawyer == null) return null;
                    if (c.LawyerId != lawyer.Id) return null;
                }
                else if (!isLawyer && c.ClientId != userId) return null;
                
                return MapToDto(c);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in GetCaseByIdAsync: {ex.Message}");
                return null;
            }
        }

        // ========== إدارة المستندات ==========

        public async Task<CaseDocumentResponseDto?> UploadDocumentAsync(Guid userId, CreateCaseDocumentDto request)
        {
            try
            {
                var caseEntity = await _context.Cases.FirstOrDefaultAsync(c => c.Id == request.CaseId);
                if (caseEntity == null) return null;
                
                bool hasAccess = false;
                
                if (caseEntity.ClientId == userId)
                {
                    hasAccess = true;
                }
                else
                {
                    var lawyer = await GetLawyerProfileByUserIdAsync(userId);
                    if (lawyer != null && caseEntity.LawyerId == lawyer.Id)
                    {
                        hasAccess = true;
                    }
                }
                
                if (!hasAccess) return null;
                
                if (request.File == null || request.File.Length == 0) return null;

                var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".txt", ".jpg", ".jpeg", ".png" };
                if (!allowedExtensions.Contains(extension) || request.File.Length > 10 * 1024 * 1024) return null;

                var uploadsFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "uploads", "cases", request.CaseId.ToString());
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create)) await request.File.CopyToAsync(stream);

                var document = new CaseDocument
                {
                    Id = Guid.NewGuid(), 
                    CaseId = request.CaseId, 
                    FileName = request.File.FileName,
                    FileUrl = $"/uploads/cases/{request.CaseId}/{fileName}", 
                    FileType = request.File.ContentType,
                    FileSize = request.File.Length, 
                    Description = request.Description, 
                    UploadedBy = userId,
                    UploadedAt = DateTime.UtcNow, 
                    IsVerified = caseEntity.LawyerId == (await GetLawyerProfileByUserIdAsync(userId))?.Id
                };

                _context.CaseDocuments.Add(document);
                await _context.SaveChangesAsync();
                
                return await GetDocumentByIdAsync(document.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in UploadDocumentAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteDocumentAsync(Guid userId, Guid documentId)
        {
            try
            {
                var document = await _context.CaseDocuments
                    .Include(d => d.Case)
                    .FirstOrDefaultAsync(d => d.Id == documentId);
                    
                if (document == null) return false;
                
                bool hasAccess = false;
                
                if (document.Case.ClientId == userId || document.UploadedBy == userId)
                {
                    hasAccess = true;
                }
                else
                {
                    var lawyer = await GetLawyerProfileByUserIdAsync(userId);
                    if (lawyer != null && document.Case.LawyerId == lawyer.Id)
                    {
                        hasAccess = true;
                    }
                }
                
                if (!hasAccess) return false;

                var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), document.FileUrl.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);

                _context.CaseDocuments.Remove(document);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in DeleteDocumentAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<byte[]?> DownloadDocumentAsync(Guid documentId)
        {
            try
            {
                var document = await _context.CaseDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
                if (document == null) return null;

                var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), document.FileUrl.TrimStart('/'));
                return File.Exists(filePath) ? await File.ReadAllBytesAsync(filePath) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in DownloadDocumentAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<CaseDocumentResponseDto?> GetDocumentByIdAsync(Guid documentId)
        {
            try
            {
                var document = await _context.CaseDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
                if (document == null) return null;

                var uploader = await _context.Users.FindAsync(document.UploadedBy);
                return new CaseDocumentResponseDto
                {
                    Id = document.Id, 
                    FileName = document.FileName, 
                    FileUrl = document.FileUrl,
                    FileType = document.FileType, 
                    FileSizeFormatted = FormatFileSize(document.FileSize),
                    Description = document.Description, 
                    UploadedByName = uploader?.FullName ?? "غير معروف",
                    UploadedAt = document.UploadedAt, 
                    IsVerified = document.IsVerified
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in GetDocumentByIdAsync: {ex.Message}");
                return null;
            }
        }

        // ========== إدارة الملاحظات ==========

        public async Task<CaseNoteResponseDto?> AddNoteAsync(Guid userId, CreateCaseNoteDto request, bool isLawyer = false)
        {
            try
            {
                var caseEntity = await _context.Cases.FirstOrDefaultAsync(c => c.Id == request.CaseId);
                if (caseEntity == null) return null;
                
                if (isLawyer)
                {
                    var lawyer = await GetLawyerProfileByUserIdAsync(userId);
                    if (lawyer == null || caseEntity.LawyerId != lawyer.Id) return null;
                }
                else if (!isLawyer && caseEntity.ClientId != userId) return null;

                var note = new CaseNote 
                { 
                    Id = Guid.NewGuid(), 
                    CaseId = request.CaseId, 
                    Content = request.Content, 
                    WrittenBy = userId, 
                    CreatedAt = DateTime.UtcNow, 
                    IsPrivate = request.IsPrivate && isLawyer 
                };
                _context.CaseNotes.Add(note);
                await _context.SaveChangesAsync();
                
                return await GetNoteByIdAsync(note.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in AddNoteAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<CaseNoteResponseDto?> UpdateNoteAsync(Guid userId, Guid noteId, string content)
        {
            try
            {
                var note = await _context.CaseNotes.FirstOrDefaultAsync(n => n.Id == noteId);
                if (note == null || note.WrittenBy != userId) return null;
                
                note.Content = content; 
                note.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                return await GetNoteByIdAsync(noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in UpdateNoteAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteNoteAsync(Guid userId, Guid noteId)
        {
            try
            {
                var note = await _context.CaseNotes.FirstOrDefaultAsync(n => n.Id == noteId);
                if (note == null || note.WrittenBy != userId) return false;
                
                _context.CaseNotes.Remove(note);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in DeleteNoteAsync: {ex.Message}");
                return false;
            }
        }

        // ========== إحصائيات ==========

        public async Task<CaseStatsDto> GetCaseStatsAsync(Guid? lawyerId = null, Guid? clientId = null)
        {
            try
            {
                var query = _context.Cases.AsQueryable();
                if (lawyerId.HasValue) query = query.Where(c => c.LawyerId == lawyerId.Value);
                if (clientId.HasValue) query = query.Where(c => c.ClientId == clientId.Value);

                var now = DateTime.UtcNow;
                var weekLater = now.AddDays(7);
                
                return new CaseStatsDto
                {
                    Total = await query.CountAsync(),
                    Active = await query.CountAsync(c => c.Status == CaseStatus.Active),
                    Pending = await query.CountAsync(c => c.Status == CaseStatus.Pending),
                    Completed = await query.CountAsync(c => c.Status == CaseStatus.Completed),
                    Rejected = await query.CountAsync(c => c.Status == CaseStatus.Rejected),
                    OnHold = await query.CountAsync(c => c.Status == CaseStatus.OnHold),
                    Urgent = await query.CountAsync(c => c.Priority == CasePriority.Urgent),
                    UpcomingHearings = await query.CountAsync(c => c.NextHearingDate.HasValue && c.NextHearingDate.Value >= now && c.NextHearingDate.Value <= weekLater)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in GetCaseStatsAsync: {ex.Message}");
                return new CaseStatsDto();
            }
        }

        public async Task<CaseStatsDto> GetCaseStatsForUserAsync(Guid userId, bool isLawyer = false)
        {
            try
            {
                IQueryable<Case> query = _context.Cases;
                
                if (isLawyer)
                {
                    var lawyer = await GetLawyerProfileByUserIdAsync(userId);
                    if (lawyer != null)
                    {
                        query = query.Where(c => c.LawyerId == lawyer.Id);
                    }
                    else
                    {
                        return new CaseStatsDto();
                    }
                }
                else
                {
                    query = query.Where(c => c.ClientId == userId);
                }

                var now = DateTime.UtcNow;
                var weekLater = now.AddDays(7);
                
                return new CaseStatsDto
                {
                    Total = await query.CountAsync(),
                    Active = await query.CountAsync(c => c.Status == CaseStatus.Active),
                    Pending = await query.CountAsync(c => c.Status == CaseStatus.Pending),
                    Completed = await query.CountAsync(c => c.Status == CaseStatus.Completed),
                    Rejected = await query.CountAsync(c => c.Status == CaseStatus.Rejected),
                    OnHold = await query.CountAsync(c => c.Status == CaseStatus.OnHold),
                    Urgent = await query.CountAsync(c => c.Priority == CasePriority.Urgent),
                    UpcomingHearings = await query.CountAsync(c => c.NextHearingDate.HasValue && c.NextHearingDate.Value >= now && c.NextHearingDate.Value <= weekLater)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in GetCaseStatsForUserAsync: {ex.Message}");
                return new CaseStatsDto();
            }
        }

        // ========== Helpers ==========

        private async Task<CaseNoteResponseDto?> GetNoteByIdAsync(Guid noteId)
        {
            var note = await _context.CaseNotes.FirstOrDefaultAsync(n => n.Id == noteId);
            if (note == null) return null;
            
            var writer = await _context.Users.FindAsync(note.WrittenBy);
            return new CaseNoteResponseDto 
            { 
                Id = note.Id, 
                Content = note.Content, 
                WrittenByName = writer?.FullName ?? "غير معروف", 
                CreatedAt = note.CreatedAt, 
                UpdatedAt = note.UpdatedAt, 
                IsPrivate = note.IsPrivate 
            };
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes; 
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) 
            { 
                order++; 
                len /= 1024; 
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private CaseResponseDto MapToDto(Case c) => new()
        {
            Id = c.Id, 
            CaseNumber = c.CaseNumber, 
            Title = c.Title, 
            Description = c.Description,
            ClientId = c.ClientId, 
            ClientName = c.Client?.FullName ?? "غير معروف",
            ClientEmail = c.Client?.Email ?? "",
            ClientPhone = c.Client?.Phone ?? "",
            LawyerId = c.LawyerId, 
            LawyerName = c.Lawyer?.User?.FullName,
            Court = c.Court, 
            NextHearingDate = c.NextHearingDate,
            Status = c.Status, 
            Priority = c.Priority, 
            CaseType = c.CaseType,
            CreatedAt = c.CreatedAt, 
            UpdatedAt = c.UpdatedAt, 
            ClosedAt = c.ClosedAt,
            DocumentsCount = c.Documents?.Count ?? 0, 
            NotesCount = c.Notes?.Count ?? 0,
            Documents = c.Documents?.Select(d => new CaseDocumentResponseDto 
            { 
                Id = d.Id, 
                FileName = d.FileName, 
                FileUrl = d.FileUrl, 
                FileType = d.FileType, 
                FileSizeFormatted = FormatFileSize(d.FileSize), 
                Description = d.Description, 
                UploadedAt = d.UploadedAt, 
                IsVerified = d.IsVerified 
            }).ToList() ?? new(),
            Notes = c.Notes?.Select(n => new CaseNoteResponseDto 
            { 
                Id = n.Id, 
                Content = n.Content, 
                CreatedAt = n.CreatedAt, 
                UpdatedAt = n.UpdatedAt, 
                IsPrivate = n.IsPrivate 
            }).ToList() ?? new()
        };
    }
}