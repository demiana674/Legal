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
            var client = await _context.Users.FirstOrDefaultAsync(u => u.UserID == request.ClientId && u.Role == UserRole.User);
            if (client == null) return null;

            Guid? lawyerProfileId = null;
            if (isLawyer)
            {
                var lawyer = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId);
                if (lawyer == null) return null;
                lawyerProfileId = lawyer.Id;
            }

            var newCase = new Case
            {
                Id = Guid.NewGuid(),
                CaseNumber = GenerateCaseNumber(),
                Title = request.Title,
                Description = request.Description,
                ClientId = request.ClientId,
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

        public async Task<CaseResponseDto?> UpdateCaseAsync(Guid userId, Guid caseId, UpdateCaseDto request, bool isLawyer = false)
        {
            var existingCase = await _context.Cases
                .Include(c => c.Client).Include(c => c.Lawyer).ThenInclude(l => l!.User)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (existingCase == null) return null;
            if (isLawyer && existingCase.LawyerId != userId) return null;
            if (!isLawyer && existingCase.ClientId != userId) return null;

            bool hasChanges = false;
            if (!string.IsNullOrEmpty(request.Title) && existingCase.Title != request.Title) { existingCase.Title = request.Title; hasChanges = true; }
            if (!string.IsNullOrEmpty(request.Description) && existingCase.Description != request.Description) { existingCase.Description = request.Description; hasChanges = true; }
            if (request.Status.HasValue && existingCase.Status != request.Status) { existingCase.Status = request.Status.Value; if (request.Status.Value == CaseStatus.Completed || request.Status.Value == CaseStatus.Rejected) existingCase.ClosedAt = DateTime.UtcNow; hasChanges = true; }
            if (request.Priority.HasValue && existingCase.Priority != request.Priority) { existingCase.Priority = request.Priority.Value; hasChanges = true; }

            if (hasChanges) { existingCase.UpdatedAt = DateTime.UtcNow; await _context.SaveChangesAsync(); }
            return await GetCaseByIdAsync(userId, caseId, isLawyer);
        }

        public async Task<bool> DeleteCaseAsync(Guid userId, Guid caseId, bool isLawyer = false)
        {
            var existingCase = await _context.Cases.Include(c => c.Documents).Include(c => c.Notes).FirstOrDefaultAsync(c => c.Id == caseId);
            if (existingCase == null) return false;
            if (isLawyer && existingCase.LawyerId != userId) return false;

            foreach (var doc in existingCase.Documents)
            {
                var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), doc.FileUrl.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }

            _context.Cases.Remove(existingCase);
            await _context.SaveChangesAsync();
            return true;
        }

        // ========== جلب القضايا ==========

        public async Task<List<CaseResponseDto>> GetCasesAsync(CaseFilterDto filter)
        {
            var query = _context.Cases.Include(c => c.Client).Include(c => c.Lawyer).ThenInclude(l => l!.User).Include(c => c.Documents).Include(c => c.Notes).AsQueryable();
            if (filter.ClientId.HasValue) query = query.Where(c => c.ClientId == filter.ClientId.Value);
            if (filter.LawyerId.HasValue) query = query.Where(c => c.LawyerId == filter.LawyerId.Value);
            if (filter.Status.HasValue) query = query.Where(c => c.Status == filter.Status.Value);
            if (!string.IsNullOrEmpty(filter.SearchTerm)) { var t = filter.SearchTerm.ToLower(); query = query.Where(c => c.Title.ToLower().Contains(t) || (c.Description != null && c.Description.ToLower().Contains(t))); }

            int page = Math.Max(1, filter.Page);
            int pageSize = Math.Max(1, Math.Min(100, filter.PageSize));
            var cases = await query.OrderByDescending(c => c.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return cases.Select(MapToDto).ToList();
        }

        public async Task<CaseResponseDto?> GetCaseByIdAsync(Guid userId, Guid caseId, bool isLawyer = false)
        {
            var c = await _context.Cases.Include(x => x.Client).Include(x => x.Lawyer).ThenInclude(l => l!.User).Include(x => x.Documents).Include(x => x.Notes).FirstOrDefaultAsync(x => x.Id == caseId);
            if (c == null) return null;
            if (isLawyer && c.LawyerId != userId) return null;
            if (!isLawyer && c.ClientId != userId) return null;
            return MapToDto(c);
        }

        // ========== إدارة المستندات ==========

        public async Task<CaseDocumentResponseDto?> UploadDocumentAsync(Guid userId, CreateCaseDocumentDto request)
        {
            var caseEntity = await _context.Cases.FirstOrDefaultAsync(c => c.Id == request.CaseId);
            if (caseEntity == null || (caseEntity.LawyerId != userId && caseEntity.ClientId != userId)) return null;
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
                Id = Guid.NewGuid(), CaseId = request.CaseId, FileName = request.File.FileName,
                FileUrl = $"/uploads/cases/{request.CaseId}/{fileName}", FileType = request.File.ContentType,
                FileSize = request.File.Length, Description = request.Description, UploadedBy = userId,
                UploadedAt = DateTime.UtcNow, IsVerified = caseEntity.LawyerId == userId
            };

            _context.CaseDocuments.Add(document);
            await _context.SaveChangesAsync();
            return await GetDocumentByIdAsync(document.Id);
        }

        public async Task<bool> DeleteDocumentAsync(Guid userId, Guid documentId)
        {
            var document = await _context.CaseDocuments.Include(d => d.Case).FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null) return false;
            if (document.Case.LawyerId != userId && document.Case.ClientId != userId && document.UploadedBy != userId) return false;

            var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), document.FileUrl.TrimStart('/'));
            if (File.Exists(filePath)) File.Delete(filePath);

            _context.CaseDocuments.Remove(document);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]?> DownloadDocumentAsync(Guid documentId)
        {
            var document = await _context.CaseDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null) return null;

            var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), document.FileUrl.TrimStart('/'));
            return File.Exists(filePath) ? await File.ReadAllBytesAsync(filePath) : null;
        }

        public async Task<CaseDocumentResponseDto?> GetDocumentByIdAsync(Guid documentId)
        {
            var document = await _context.CaseDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null) return null;

            var uploader = await _context.Users.FindAsync(document.UploadedBy);
            return new CaseDocumentResponseDto
            {
                Id = document.Id, FileName = document.FileName, FileUrl = document.FileUrl,
                FileType = document.FileType, FileSizeFormatted = FormatFileSize(document.FileSize),
                Description = document.Description, UploadedByName = uploader?.FullName ?? "غير معروف",
                UploadedAt = document.UploadedAt, IsVerified = document.IsVerified
            };
        }

        // ========== إدارة الملاحظات ==========

        public async Task<CaseNoteResponseDto?> AddNoteAsync(Guid userId, CreateCaseNoteDto request, bool isLawyer = false)
        {
            var caseEntity = await _context.Cases.FirstOrDefaultAsync(c => c.Id == request.CaseId);
            if (caseEntity == null) return null;
            if (isLawyer && caseEntity.LawyerId != userId) return null;
            if (!isLawyer && caseEntity.ClientId != userId) return null;

            var note = new CaseNote { Id = Guid.NewGuid(), CaseId = request.CaseId, Content = request.Content, WrittenBy = userId, CreatedAt = DateTime.UtcNow, IsPrivate = request.IsPrivate && isLawyer };
            _context.CaseNotes.Add(note);
            await _context.SaveChangesAsync();
            return await GetNoteByIdAsync(note.Id);
        }

        public async Task<CaseNoteResponseDto?> UpdateNoteAsync(Guid userId, Guid noteId, string content)
        {
            var note = await _context.CaseNotes.FirstOrDefaultAsync(n => n.Id == noteId);
            if (note == null || note.WrittenBy != userId) return null;
            note.Content = content; note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await GetNoteByIdAsync(noteId);
        }

        public async Task<bool> DeleteNoteAsync(Guid userId, Guid noteId)
        {
            var note = await _context.CaseNotes.FirstOrDefaultAsync(n => n.Id == noteId);
            if (note == null || note.WrittenBy != userId) return false;
            _context.CaseNotes.Remove(note);
            await _context.SaveChangesAsync();
            return true;
        }

        // ========== إحصائيات ==========

        public async Task<CaseStatsDto> GetCaseStatsAsync(Guid? lawyerId = null, Guid? clientId = null)
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

        // ========== Helpers ==========

        private string GenerateCaseNumber() => $"CS-{DateTime.UtcNow.Year}-{(_context.Cases.Count() + 1):D6}";

        private async Task<CaseNoteResponseDto?> GetNoteByIdAsync(Guid noteId)
        {
            var note = await _context.CaseNotes.FirstOrDefaultAsync(n => n.Id == noteId);
            if (note == null) return null;
            var writer = await _context.Users.FindAsync(note.WrittenBy);
            return new CaseNoteResponseDto { Id = note.Id, Content = note.Content, WrittenByName = writer?.FullName ?? "غير معروف", CreatedAt = note.CreatedAt, UpdatedAt = note.UpdatedAt, IsPrivate = note.IsPrivate };
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes; int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
            return $"{len:0.##} {sizes[order]}";
        }

        private CaseResponseDto MapToDto(Case c) => new()
        {
            Id = c.Id, CaseNumber = c.CaseNumber, Title = c.Title, Description = c.Description,
            ClientId = c.ClientId, ClientName = c.Client?.FullName ?? "غير معروف",
            LawyerId = c.LawyerId, LawyerName = c.Lawyer?.User?.FullName,
            Court = c.Court, NextHearingDate = c.NextHearingDate,
            Status = c.Status, Priority = c.Priority, CaseType = c.CaseType,
            CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt, ClosedAt = c.ClosedAt,
            DocumentsCount = c.Documents?.Count ?? 0, NotesCount = c.Notes?.Count ?? 0,
            Documents = c.Documents?.Select(d => new CaseDocumentResponseDto { Id = d.Id, FileName = d.FileName, FileUrl = d.FileUrl, FileType = d.FileType, FileSizeFormatted = FormatFileSize(d.FileSize), Description = d.Description, UploadedAt = d.UploadedAt, IsVerified = d.IsVerified }).ToList() ?? new(),
            Notes = c.Notes?.Select(n => new CaseNoteResponseDto { Id = n.Id, Content = n.Content, CreatedAt = n.CreatedAt, UpdatedAt = n.UpdatedAt, IsPrivate = n.IsPrivate }).ToList() ?? new()
        };
    }
}