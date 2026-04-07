// LegalMateAI.BLL/Services/Service/CaseService.cs
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
using System.Text;

namespace LegalMateAI.BLL.Services.Service
{
    public class CaseService : ICaseService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CaseService> _logger;

        public CaseService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CaseService> logger)
        {
            _context = context;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // ========== CRUD للقضايا ==========

        public async Task<CaseResponseDto?> CreateCaseAsync(Guid userId, CreateCaseDto request, bool isLawyer = false)
        {
            _logger.LogInformation($"CreateCaseAsync called by UserId: {userId}, IsLawyer: {isLawyer}");

            // التحقق من وجود الموكل
            var client = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == request.ClientId && u.Role == UserRole.User);
            
            if (client == null)
            {
                _logger.LogWarning($"Client not found: {request.ClientId}");
                return null;
            }

            // التحقق من أن المحامي (إذا كان موجود) يضيف قضية لموكله
            if (isLawyer)
            {
                var lawyer = await _context.LawyerProfiles
                    .FirstOrDefaultAsync(l => l.UserId == userId);
                
                if (lawyer == null)
                {
                    _logger.LogWarning($"Lawyer not found for user: {userId}");
                    return null;
                }
            }

            var caseNumber = GenerateCaseNumber();

            var newCase = new Case
            {
                Id = Guid.NewGuid(),
                CaseNumber = caseNumber,
                Title = request.Title,
                Description = request.Description,
                ClientId = request.ClientId,
                LawyerId = isLawyer ? userId : (Guid?)null,
                Court = request.Court,
                NextHearingDate = request.NextHearingDate,
                Status = request.Status,
                Priority = request.Priority,
                CaseType = request.CaseType,
                CreatedAt = DateTime.UtcNow
            };

            _context.Cases.Add(newCase);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Case created successfully: {caseNumber}");

            return await GetCaseByIdAsync(userId, newCase.Id, isLawyer);
        }

        public async Task<CaseResponseDto?> UpdateCaseAsync(Guid userId, Guid caseId, UpdateCaseDto request, bool isLawyer = false)
        {
            _logger.LogInformation($"UpdateCaseAsync called: CaseId={caseId}, UserId={userId}");

            var existingCase = await _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .ThenInclude(l => l!.User)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (existingCase == null)
            {
                _logger.LogWarning($"Case not found: {caseId}");
                return null;
            }

            // التحقق من الصلاحية
            if (isLawyer && existingCase.LawyerId != userId)
            {
                _logger.LogWarning($"User {userId} is not the assigned lawyer for case {caseId}");
                return null;
            }

            if (!isLawyer && existingCase.ClientId != userId)
            {
                _logger.LogWarning($"User {userId} is not the client for case {caseId}");
                return null;
            }

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

            if (!string.IsNullOrEmpty(request.Court) && existingCase.Court != request.Court)
            {
                existingCase.Court = request.Court;
                hasChanges = true;
            }

            if (request.NextHearingDate.HasValue && existingCase.NextHearingDate != request.NextHearingDate)
            {
                existingCase.NextHearingDate = request.NextHearingDate;
                hasChanges = true;
            }

            if (request.Status.HasValue && existingCase.Status != request.Status)
            {
                existingCase.Status = request.Status.Value;
                if (request.Status.Value == CaseStatus.Completed || request.Status.Value == CaseStatus.Rejected)
                {
                    existingCase.ClosedAt = DateTime.UtcNow;
                }
                hasChanges = true;
            }

            if (request.Priority.HasValue && existingCase.Priority != request.Priority)
            {
                existingCase.Priority = request.Priority.Value;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.CaseType) && existingCase.CaseType != request.CaseType)
            {
                existingCase.CaseType = request.CaseType;
                hasChanges = true;
            }

            if (hasChanges)
            {
                existingCase.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Case {caseId} updated successfully");
            }

            return await GetCaseByIdAsync(userId, caseId, isLawyer);
        }

        public async Task<bool> DeleteCaseAsync(Guid userId, Guid caseId, bool isLawyer = false)
        {
            _logger.LogInformation($"DeleteCaseAsync called: CaseId={caseId}, UserId={userId}");

            var existingCase = await _context.Cases
                .Include(c => c.Documents)
                .Include(c => c.Notes)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (existingCase == null)
            {
                _logger.LogWarning($"Case not found: {caseId}");
                return false;
            }

            // التحقق من الصلاحية (فقط المحامي المخصص أو الأدمن يمكنه الحذف)
            if (isLawyer && existingCase.LawyerId != userId)
            {
                _logger.LogWarning($"User {userId} is not authorized to delete case {caseId}");
                return false;
            }

            // حذف المستندات الفعلية من السيرفر
            foreach (var doc in existingCase.Documents)
            {
                var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), 
                    doc.FileUrl.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            _context.Cases.Remove(existingCase);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Case {caseId} deleted successfully");
            return true;
        }

        // ========== جلب القضايا ==========

        public async Task<List<CaseResponseDto>> GetCasesAsync(CaseFilterDto filter)
        {
            _logger.LogInformation($"GetCasesAsync called with filter: {@filter}");

            var query = _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(c => c.Documents)
                .Include(c => c.Notes)
                .AsQueryable();

            // تطبيق الفلاتر
            if (filter.ClientId.HasValue)
            {
                query = query.Where(c => c.ClientId == filter.ClientId.Value);
            }

            if (filter.LawyerId.HasValue)
            {
                query = query.Where(c => c.LawyerId == filter.LawyerId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(c => c.Status == filter.Status.Value);
            }

            if (filter.Priority.HasValue)
            {
                query = query.Where(c => c.Priority == filter.Priority.Value);
            }

            if (!string.IsNullOrEmpty(filter.CaseType))
            {
                query = query.Where(c => c.CaseType != null && c.CaseType.Contains(filter.CaseType));
            }

            if (!string.IsNullOrEmpty(filter.Court))
            {
                query = query.Where(c => c.Court != null && c.Court.Contains(filter.Court));
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(c => 
                    c.Title.ToLower().Contains(term) ||
                    (c.CaseNumber != null && c.CaseNumber.ToLower().Contains(term)) ||
                    (c.Description != null && c.Description.ToLower().Contains(term)));
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(c => c.CreatedAt <= filter.ToDate.Value);
            }

            if (filter.NextHearingFrom.HasValue)
            {
                query = query.Where(c => c.NextHearingDate >= filter.NextHearingFrom.Value);
            }

            if (filter.NextHearingTo.HasValue)
            {
                query = query.Where(c => c.NextHearingDate <= filter.NextHearingTo.Value);
            }

            int page = Math.Max(1, filter.Page);
            int pageSize = Math.Max(1, Math.Min(100, filter.PageSize));

            var cases = await query
                .OrderByDescending(c => c.Priority)
                .ThenByDescending(c => c.NextHearingDate)
                .ThenByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return cases.Select(c => MapToDto(c)).ToList();
        }

        public async Task<CaseResponseDto?> GetCaseByIdAsync(Guid userId, Guid caseId, bool isLawyer = false)
        {
            _logger.LogInformation($"GetCaseByIdAsync called: CaseId={caseId}, UserId={userId}");

            var caseEntity = await _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(c => c.Documents)
                .Include(c => c.Notes)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseEntity == null)
            {
                _logger.LogWarning($"Case not found: {caseId}");
                return null;
            }

            // التحقق من الصلاحية
            if (isLawyer && caseEntity.LawyerId != userId)
            {
                _logger.LogWarning($"User {userId} is not the assigned lawyer for case {caseId}");
                return null;
            }

            if (!isLawyer && caseEntity.ClientId != userId)
            {
                _logger.LogWarning($"User {userId} is not the client for case {caseId}");
                return null;
            }

            return MapToDto(caseEntity);
        }

        // ========== إدارة المستندات ==========

        public async Task<CaseDocumentResponseDto?> UploadDocumentAsync(Guid userId, CreateCaseDocumentDto request)
        {
            _logger.LogInformation($"UploadDocumentAsync called: CaseId={request.CaseId}, UserId={userId}");

            var caseEntity = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == request.CaseId);

            if (caseEntity == null)
            {
                _logger.LogWarning($"Case not found: {request.CaseId}");
                return null;
            }

            // التحقق من الصلاحية (المحامي المخصص أو الموكل)
            if (caseEntity.LawyerId != userId && caseEntity.ClientId != userId)
            {
                _logger.LogWarning($"User {userId} is not authorized to upload documents for case {request.CaseId}");
                return null;
            }

            if (request.File == null || request.File.Length == 0)
            {
                _logger.LogWarning("No file provided");
                return null;
            }

            var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".txt", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning($"Invalid file extension: {extension}");
                return null;
            }

            if (request.File.Length > 10 * 1024 * 1024) // 10MB
            {
                _logger.LogWarning($"File too large: {request.File.Length} bytes");
                return null;
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), 
                "uploads", "cases", request.CaseId.ToString());
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/cases/{request.CaseId}/{fileName}";

            var document = new CaseDocument
            {
                Id = Guid.NewGuid(),
                CaseId = request.CaseId,
                FileName = request.File.FileName,
                FileUrl = fileUrl,
                FileType = request.File.ContentType,
                FileSize = request.File.Length,
                Description = request.Description,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow,
                IsVerified = caseEntity.LawyerId == userId
            };

            _context.CaseDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Document uploaded successfully: {document.FileName}");

            return await GetDocumentByIdAsync(document.Id);
        }

        public async Task<bool> DeleteDocumentAsync(Guid userId, Guid documentId)
        {
            _logger.LogInformation($"DeleteDocumentAsync called: DocumentId={documentId}, UserId={userId}");

            var document = await _context.CaseDocuments
                .Include(d => d.Case)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                _logger.LogWarning($"Document not found: {documentId}");
                return false;
            }

            // التحقق من الصلاحية
            var caseEntity = document.Case;
            if (caseEntity.LawyerId != userId && caseEntity.ClientId != userId && document.UploadedBy != userId)
            {
                _logger.LogWarning($"User {userId} is not authorized to delete document {documentId}");
                return false;
            }

            // حذف الملف الفعلي
            var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), 
                document.FileUrl.TrimStart('/'));
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _context.CaseDocuments.Remove(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Document deleted successfully: {documentId}");
            return true;
        }

        public async Task<byte[]?> DownloadDocumentAsync(Guid userId, Guid documentId)
        {
            _logger.LogInformation($"DownloadDocumentAsync called: DocumentId={documentId}, UserId={userId}");

            var document = await _context.CaseDocuments
                .Include(d => d.Case)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                _logger.LogWarning($"Document not found: {documentId}");
                return null;
            }

            // التحقق من الصلاحية
            var caseEntity = document.Case;
            if (caseEntity.LawyerId != userId && caseEntity.ClientId != userId)
            {
                _logger.LogWarning($"User {userId} is not authorized to download document {documentId}");
                return null;
            }

            var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), 
                document.FileUrl.TrimStart('/'));

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"File not found: {filePath}");
                return null;
            }

            return await File.ReadAllBytesAsync(filePath);
        }

        // ✅ دالة GetDocumentByIdAsync (مرة واحدة فقط)
        public async Task<CaseDocumentResponseDto?> GetDocumentByIdAsync(Guid documentId)
        {
            _logger.LogInformation($"GetDocumentByIdAsync called: DocumentId={documentId}");

            var document = await _context.CaseDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                _logger.LogWarning($"Document not found: {documentId}");
                return null;
            }

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

        // ========== إدارة الملاحظات ==========

        public async Task<CaseNoteResponseDto?> AddNoteAsync(Guid userId, CreateCaseNoteDto request, bool isLawyer = false)
        {
            _logger.LogInformation($"AddNoteAsync called: CaseId={request.CaseId}, UserId={userId}");

            var caseEntity = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == request.CaseId);

            if (caseEntity == null)
            {
                _logger.LogWarning($"Case not found: {request.CaseId}");
                return null;
            }

            // التحقق من الصلاحية
            if (isLawyer && caseEntity.LawyerId != userId)
            {
                _logger.LogWarning($"User {userId} is not the assigned lawyer for case {request.CaseId}");
                return null;
            }

            if (!isLawyer && caseEntity.ClientId != userId)
            {
                _logger.LogWarning($"User {userId} is not the client for case {request.CaseId}");
                return null;
            }

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

            _logger.LogInformation($"Note added successfully for case {request.CaseId}");

            return await GetNoteByIdAsync(note.Id, userId, isLawyer);
        }

        public async Task<CaseNoteResponseDto?> UpdateNoteAsync(Guid userId, Guid noteId, string content)
        {
            _logger.LogInformation($"UpdateNoteAsync called: NoteId={noteId}, UserId={userId}");

            var note = await _context.CaseNotes
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                _logger.LogWarning($"Note not found: {noteId}");
                return null;
            }

            if (note.WrittenBy != userId)
            {
                _logger.LogWarning($"User {userId} is not the author of note {noteId}");
                return null;
            }

            note.Content = content;
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Note {noteId} updated successfully");

            return await GetNoteByIdAsync(noteId, userId, false);
        }

        public async Task<bool> DeleteNoteAsync(Guid userId, Guid noteId)
        {
            _logger.LogInformation($"DeleteNoteAsync called: NoteId={noteId}, UserId={userId}");

            var note = await _context.CaseNotes
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                _logger.LogWarning($"Note not found: {noteId}");
                return false;
            }

            if (note.WrittenBy != userId)
            {
                _logger.LogWarning($"User {userId} is not the author of note {noteId}");
                return false;
            }

            _context.CaseNotes.Remove(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Note {noteId} deleted successfully");
            return true;
        }

        // ========== إحصائيات ==========

        public async Task<CaseStatsDto> GetCaseStatsAsync(Guid? lawyerId = null, Guid? clientId = null)
        {
            _logger.LogInformation($"GetCaseStatsAsync called: LawyerId={lawyerId}, ClientId={clientId}");

            var query = _context.Cases.AsQueryable();

            if (lawyerId.HasValue)
            {
                query = query.Where(c => c.LawyerId == lawyerId.Value);
            }

            if (clientId.HasValue)
            {
                query = query.Where(c => c.ClientId == clientId.Value);
            }

            var now = DateTime.UtcNow;
            var weekLater = now.AddDays(7);

            var stats = new CaseStatsDto
            {
                Total = await query.CountAsync(),
                Active = await query.CountAsync(c => c.Status == CaseStatus.Active),
                Pending = await query.CountAsync(c => c.Status == CaseStatus.Pending),
                Completed = await query.CountAsync(c => c.Status == CaseStatus.Completed),
                Rejected = await query.CountAsync(c => c.Status == CaseStatus.Rejected),
                OnHold = await query.CountAsync(c => c.Status == CaseStatus.OnHold),
                Urgent = await query.CountAsync(c => c.Priority == CasePriority.Urgent),
                UpcomingHearings = await query.CountAsync(c => 
                    c.NextHearingDate.HasValue && 
                    c.NextHearingDate.Value >= now && 
                    c.NextHearingDate.Value <= weekLater)
            };

            return stats;
        }

        // ========== Helper Methods ==========

        private string GenerateCaseNumber()
        {
            var year = DateTime.UtcNow.Year;
            var count = _context.Cases.Count() + 1;
            return $"CS-{year}-{count:D6}";
        }

        private async Task<CaseNoteResponseDto?> GetNoteByIdAsync(Guid noteId, Guid userId, bool isLawyer)
        {
            var note = await _context.CaseNotes
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null) return null;

            // التحقق من أن المستخدم يرى الملاحظات الخاصة فقط إذا كان المحامي
            if (note.IsPrivate && !isLawyer)
            {
                _logger.LogWarning($"User {userId} attempted to view private note {noteId}");
                return null;
            }

            var writer = await _context.Users.FindAsync(note.WrittenBy);

            return new CaseNoteResponseDto
            {
                Id = note.Id,
                Content = note.Content,
                WrittenByName = writer?.FullName ?? "غير معروف",
                WrittenByRole = writer?.Role.ToString(),
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt,
                IsPrivate = note.IsPrivate
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private CaseResponseDto MapToDto(Case caseEntity)
        {
            return new CaseResponseDto
            {
                Id = caseEntity.Id,
                CaseNumber = caseEntity.CaseNumber,
                Title = caseEntity.Title,
                Description = caseEntity.Description,
                ClientId = caseEntity.ClientId,
                ClientName = caseEntity.Client?.FullName ?? "غير معروف",
                ClientEmail = caseEntity.Client?.Email,
                LawyerId = caseEntity.LawyerId,
                LawyerName = caseEntity.Lawyer?.User?.FullName,
                Court = caseEntity.Court,
                NextHearingDate = caseEntity.NextHearingDate,
                Status = caseEntity.Status,
                Priority = caseEntity.Priority,
                CaseType = caseEntity.CaseType,
                CreatedAt = caseEntity.CreatedAt,
                UpdatedAt = caseEntity.UpdatedAt,
                ClosedAt = caseEntity.ClosedAt,
                DocumentsCount = caseEntity.Documents?.Count ?? 0,
                NotesCount = caseEntity.Notes?.Count ?? 0,
                Documents = caseEntity.Documents?.Select(d => new CaseDocumentResponseDto
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
                Notes = caseEntity.Notes?.Select(n => new CaseNoteResponseDto
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
}