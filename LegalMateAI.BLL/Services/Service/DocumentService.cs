using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;  // ✅ مهم لـ ILogger
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.BLL.Services.IService;

namespace LegalMateAI.BLL.Services.Service
{
    public class DocumentService : IDocumentService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            ILogger<DocumentService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        public async Task<DocumentResponseDto?> UploadDocumentAsync(Guid userId, CreateDocumentDto request)
        {
            try
            {
                // Validate file extension
                var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".txt" };
                var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("Invalid file type: {Extension} for user {UserId}", extension, userId);
                    return null;
                }

                // Validate file size (10MB max)
                var maxSize = 10 * 1024 * 1024;
                if (request.File.Length > maxSize)
                {
                    _logger.LogWarning("File too large: {Size} bytes for user {UserId}", request.File.Length, userId);
                    return null;
                }

                // ✅ إصلاح CA2022 - استخدام ReadAsync مع Memory<byte>
                var fileContent = new byte[request.File.Length];
                using (var stream = request.File.OpenReadStream())
                {
                    var memory = new Memory<byte>(fileContent);
                    await stream.ReadAsync(memory);
                }

                // Save file to disk
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "uploads", userId.ToString());
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                await File.WriteAllBytesAsync(filePath, fileContent);

                var fileUrl = $"/uploads/{userId}/{uniqueFileName}";

                // Save to database
                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FileName = request.File.FileName,
                    FileUrl = fileUrl,
                    FileType = request.File.ContentType,
                    FileSize = request.File.Length,
                    DocType = GetDocumentType(extension),
                    Description = request.Description,
                    Status = DocumentStatus.Pending,
                    UploadedAt = DateTime.UtcNow
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document uploaded: {DocumentId} by user {UserId}", document.Id, userId);

                return MapToDto(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<DocumentResponseDto>> GetUserDocumentsAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var documents = await _context.Documents
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return documents.Select(MapToDto).ToList();
        }

        public async Task<DocumentResponseDto?> GetDocumentByIdAsync(Guid userId, Guid documentId)
        {
            var document = await _context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            return document == null ? null : MapToDto(document);
        }

        public async Task<bool> DeleteDocumentAsync(Guid userId, Guid documentId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
                return false;

            // Delete physical file
            var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), 
                document.FileUrl.TrimStart('/'));
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document deleted: {DocumentId} by user {UserId}", documentId, userId);
            return true;
        }

        public async Task<byte[]?> DownloadDocumentAsync(Guid userId, Guid documentId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
                return null;

            var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), 
                document.FileUrl.TrimStart('/'));

            if (!File.Exists(filePath))
                return null;

            return await File.ReadAllBytesAsync(filePath);
        }

        #region Private Methods

        private DocumentType GetDocumentType(string extension)
        {
            return extension switch
            {
                ".pdf" => DocumentType.Contract,
                ".docx" or ".doc" => DocumentType.Contract,
                ".txt" => DocumentType.Other,
                _ => DocumentType.Other
            };
        }

        private DocumentResponseDto MapToDto(Document document)
        {
            return new DocumentResponseDto
            {
                Id = document.Id,
                FileName = document.FileName,
                FileUrl = document.FileUrl,
                FileType = document.FileType ?? "",
                FileSizeFormatted = FormatFileSize(document.FileSize),
                DocType = document.DocType,
                Description = document.Description,
                Status = document.Status,
                UploadedAt = document.UploadedAt,
                HasAnalysis = _context.DocumentAnalyses.Any(a => a.DocumentId == document.Id && a.Status == AnalysisStatus.Completed),
                UserId = document.UserId
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

        #endregion
    }
}