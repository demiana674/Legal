// LegalMateAI.BLL/Services/Service/LawService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.BLL.Services.IService;

namespace LegalMateAI.BLL.Services.Service
{
    public class LawService : ILawService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LawService> _logger;
        private readonly HttpClient _httpClient;

        public LawService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            ILogger<LawService> logger)
        {
            _context = context;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        // ========== للجميع ==========
        public async Task<List<LawDto>> GetLawsAsync(LawCategory? category = null, string? search = null)
        {
            var query = _context.Laws
                .Include(l => l.AddedByAdmin)
                .Include(l => l.UploadedByUser)
                .Where(l => l.IsActive && l.IsApproved)
                .AsQueryable();

            if (category.HasValue) query = query.Where(l => l.Category == category.Value);
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(l =>
                    l.Name.ToLower().Contains(search) ||
                    (l.Description != null && l.Description.ToLower().Contains(search)) ||
                    (l.SearchKeywords != null && l.SearchKeywords.ToLower().Contains(search)) ||
                    (l.LawNumber != null && l.LawNumber.ToLower().Contains(search)));
            }

            var laws = await query.OrderBy(l => l.Category).ThenBy(l => l.Name).ToListAsync();
            foreach (var law in laws) law.ViewCount++;
            await _context.SaveChangesAsync();
            return laws.Select(MapToDto).ToList();
        }

        public async Task<List<LawDto>> SearchLawsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return new List<LawDto>();
            searchTerm = searchTerm.ToLower().Trim();
            var searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var laws = await _context.Laws.Include(l => l.AddedByAdmin).Include(l => l.UploadedByUser)
                .Where(l => l.IsActive && l.IsApproved).ToListAsync();
            return laws.Where(l => CalculateMatchScore(l, searchTerm, searchWords) > 0)
                .OrderByDescending(l => CalculateMatchScore(l, searchTerm, searchWords))
                .Select(MapToDto).ToList();
        }

        public async Task<LawDto?> GetLawByIdAsync(Guid id)
        {
            var law = await _context.Laws.Include(l => l.AddedByAdmin).Include(l => l.UploadedByUser)
                .FirstOrDefaultAsync(l => l.Id == id && l.IsActive && l.IsApproved);
            if (law == null) return null;
            law.ViewCount++; await _context.SaveChangesAsync();
            return MapToDto(law);
        }

        public async Task<byte[]?> DownloadLawAsync(Guid id)
        {
            var law = await _context.Laws.FindAsync(id);
            if (law == null || !law.IsActive || !law.IsApproved) return null;
            law.DownloadCount++; await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(law.PdfFileUrl) && law.PdfFileUrl.StartsWith("http"))
            {
                try { return await _httpClient.GetByteArrayAsync(law.PdfFileUrl); }
                catch (Exception ex) { _logger.LogWarning(ex, $"Failed to download PDF: {law.PdfFileUrl}"); }
            }

            if (!string.IsNullOrEmpty(law.PdfFileUrl) && law.PdfFileUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", law.PdfFileUrl.TrimStart('/'));
                if (File.Exists(filePath)) return await File.ReadAllBytesAsync(filePath);
            }
            return null;
        }

        public async Task<object?> GetLawDownloadInfoAsync(Guid id)
        {
            var law = await _context.Laws.FindAsync(id);
            if (law == null || !law.IsActive || !law.IsApproved) return null;
            law.DownloadCount++; await _context.SaveChangesAsync();
            return new { lawId = law.Id, lawName = law.Name, hasPdf = !string.IsNullOrEmpty(law.PdfFileUrl), pdfUrl = law.PdfFileUrl, hasSourceUrl = !string.IsNullOrEmpty(law.SourceUrl), sourceUrl = law.SourceUrl };
        }

        public async Task<string?> GetLawDownloadUrlAsync(Guid id)
        {
            var law = await _context.Laws.FindAsync(id);
            if (law == null || !law.IsActive || !law.IsApproved) return null;
            law.DownloadCount++; await _context.SaveChangesAsync();
            return !string.IsNullOrEmpty(law.PdfFileUrl) ? law.PdfFileUrl : law.SourceUrl;
        }

        public async Task<List<LawCategoryDto>> GetLawCategoriesAsync()
        {
            var categories = await _context.Laws.Where(l => l.IsActive && l.IsApproved).Select(l => l.Category).Distinct().ToListAsync();
            return categories.Select(c => new LawCategoryDto { Category = c, Name = GetCategoryName(c), Count = _context.Laws.Count(l => l.Category == c && l.IsActive && l.IsApproved) }).OrderBy(c => c.Name).ToList();
        }

        // ========== للمستخدمين المسجلين ==========
        public async Task<LawDto?> UploadLawByUserWithParserAsync(Guid userId, CreateLawRequestDto request)
        {
            _logger.LogWarning("LawParserService is not available. Uploading with basic data only.");
            var law = new Law
            {
                Id = Guid.NewGuid(), Name = request.Name ?? "قانون غير معروف",
                LawNumber = request.LawNumber, Year = request.Year,
                Category = request.Category ?? LawCategory.Other,
                Description = request.Description, SourceUrl = request.SourceUrl,
                SearchKeywords = request.SearchKeywords,
                IsActive = false, IsApproved = false,
                CreatedAt = DateTime.UtcNow, UploadedByUserId = userId
            };
            _context.Laws.Add(law); await _context.SaveChangesAsync();
            return await Task.FromResult(MapToDto(law));
        }

        public async Task<LawDto?> UploadLawByUserAsync(Guid userId, AddLawDto request)
        {
            if (!request.PdfFile.ContentType.Contains("pdf")) return null;
            var folderName = GetFolderNameByCategory(request.Category);
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var lawsFolder = Path.Combine(webRootPath, "uploads", "laws", folderName);
            Directory.CreateDirectory(lawsFolder);

            var safeName = GenerateSafeFileName(request.Name, request.LawNumber, request.Year);
            var filePath = Path.Combine(lawsFolder, safeName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await request.PdfFile.CopyToAsync(stream); }

            var law = new Law
            {
                Id = Guid.NewGuid(), Name = request.Name, LawNumber = request.LawNumber,
                Year = request.Year, Category = request.Category, Description = request.Description,
                PdfFileUrl = $"/uploads/laws/{folderName}/{safeName}", SourceUrl = request.SourceUrl,
                SearchKeywords = request.SearchKeywords, IsActive = false, IsApproved = false,
                CreatedAt = DateTime.UtcNow, UploadedByUserId = userId
            };
            _context.Laws.Add(law); await _context.SaveChangesAsync();
            return await Task.FromResult(MapToDto(law));
        }

        public async Task<List<LawDto>> GetUserUploadedLawsAsync(Guid userId)
        {
            return await _context.Laws.Where(l => l.UploadedByUserId == userId)
                .OrderByDescending(l => l.CreatedAt).Select(l => MapToDto(l)).ToListAsync();
        }

        // ========== للأدمن فقط ==========
        public async Task<LawDto?> AddLawAsync(Guid adminId, IFormFile pdfFile, string name, LawCategory category, string? lawNumber, int? year, string? description, string? sourceUrl, string? searchKeywords)
        {
            var folderName = GetFolderNameByCategory(category);
            var webRootPath = _env.WebRootPath ?? "wwwroot";
            var lawsFolder = Path.Combine(webRootPath, "uploads", "laws", folderName);
            Directory.CreateDirectory(lawsFolder);
            var safeName = GenerateSafeFileName(name, lawNumber, year);
            var filePath = Path.Combine(lawsFolder, safeName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await pdfFile.CopyToAsync(stream); }

            var law = new Law
            {
                Id = Guid.NewGuid(), Name = name, LawNumber = lawNumber, Year = year,
                Category = category, Description = description,
                PdfFileUrl = $"/uploads/laws/{folderName}/{safeName}", SourceUrl = sourceUrl,
                SearchKeywords = searchKeywords, IsActive = true, IsApproved = true,
                CreatedAt = DateTime.UtcNow, AddedByAdminId = adminId, ApprovedByAdminId = adminId, ApprovedAt = DateTime.UtcNow
            };
            _context.Laws.Add(law); await _context.SaveChangesAsync();
            return MapToDto(law);
        }

        public async Task<LawDto?> UpdateLawAsync(Guid adminId, Guid lawId, UpdateLawDto request)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return null;
            if (request.Name != null) law.Name = request.Name;
            if (request.Category.HasValue) law.Category = request.Category.Value;
            if (request.LawNumber != null) law.LawNumber = request.LawNumber;
            if (request.Year.HasValue) law.Year = request.Year;
            if (request.Description != null) law.Description = request.Description;
            if (request.SourceUrl != null) law.SourceUrl = request.SourceUrl;
            if (request.SearchKeywords != null) law.SearchKeywords = request.SearchKeywords;
            if (request.IsActive.HasValue) law.IsActive = request.IsActive.Value;
            await _context.SaveChangesAsync();
            return MapToDto(law);
        }

        public async Task<bool> DeleteLawAsync(Guid adminId, Guid lawId)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;
            if (!string.IsNullOrEmpty(law.PdfFileUrl) && law.PdfFileUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", law.PdfFileUrl.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            _context.Laws.Remove(law); await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<LawDto>> GetAllLawsForAdminAsync()
        {
            return await _context.Laws.OrderByDescending(l => l.CreatedAt).Select(l => MapToDto(l)).ToListAsync();
        }

        public async Task<List<LawDto>> GetPendingLawsAsync()
        {
            return await _context.Laws.Where(l => !l.IsApproved && l.UploadedByUserId != null).OrderByDescending(l => l.CreatedAt).Select(l => MapToDto(l)).ToListAsync();
        }

        public async Task<List<LawDto>> GetPendingLawsForAdminAsync() => await GetPendingLawsAsync();

        public async Task<bool> ApproveLawAsync(Guid adminId, Guid lawId)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;
            law.IsApproved = true; law.IsActive = true; law.ApprovedByAdminId = adminId; law.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(); return true;
        }

        public async Task<bool> ApproveUserLawAsync(Guid adminId, Guid lawId) => await ApproveLawAsync(adminId, lawId);

        public async Task<bool> RejectLawAsync(Guid adminId, Guid lawId, string reason)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;
            law.IsApproved = false; law.IsActive = false; law.RejectionReason = reason;
            await _context.SaveChangesAsync(); return true;
        }

        public async Task<bool> RejectUserLawAsync(Guid adminId, Guid lawId, string reason) => await RejectLawAsync(adminId, lawId, reason);

        // ========== Helpers ==========
        private int CalculateMatchScore(Law law, string searchTerm, string[] searchWords)
        {
            int score = 0;
            var nameLower = law.Name.ToLower();
            if (nameLower.Contains(searchTerm)) score += 50;
            if (law.Description?.ToLower().Contains(searchTerm) == true) score += 20;
            if (law.SearchKeywords?.ToLower().Contains(searchTerm) == true) score += 30;
            if (law.LawNumber?.ToLower().Contains(searchTerm) == true) score += 40;
            foreach (var word in searchWords) { if (nameLower.Contains(word)) score += 10; }
            return score;
        }

        private string GenerateSafeFileName(string lawName, string? lawNumber, int? year)
        {
            var safeName = new string(lawName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Replace(" ", "_");
            if (!string.IsNullOrEmpty(lawNumber)) safeName = $"{safeName}_رقم_{lawNumber}";
            if (year.HasValue) safeName = $"{safeName}_لسنة_{year}";
            if (safeName.Length > 100) safeName = safeName[..100];
            return $"{safeName}.pdf";
        }

        private string GetFolderNameByCategory(LawCategory category) => category switch
        {
            LawCategory.Civil => "civil", LawCategory.Criminal => "criminal",
            LawCategory.Commercial => "commercial", LawCategory.Family => "family",
            _ => "other"
        };

        private string GetCategoryName(LawCategory category) => category switch
        {
            LawCategory.Constitutional => "دستوري", LawCategory.Civil => "مدني",
            LawCategory.Criminal => "جنائي", LawCategory.Commercial => "تجاري",
            LawCategory.Family => "أحوال شخصية", LawCategory.Labor => "عمل",
            LawCategory.Tax => "ضريبي", _ => category.ToString()
        };

        private LawDto MapToDto(Law law) => new()
        {
            Id = law.Id, Name = law.Name, LawNumber = law.LawNumber,
            Year = law.Year, Category = law.Category,
            CategoryName = GetCategoryName(law.Category),
            Description = law.Description, PdfFileUrl = law.PdfFileUrl,
            SourceUrl = law.SourceUrl,
            SearchKeywords = law.SearchKeywords?.Split(',').Select(k => k.Trim()).ToList() ?? new(),
            DownloadCount = law.DownloadCount, ViewCount = law.ViewCount,
            IsActive = law.IsActive, IsApproved = law.IsApproved,
            CreatedAt = law.CreatedAt,
            AddedByAdminName = law.AddedByAdmin?.FullName ?? (law.UploadedByUser?.FullName ?? "غير معروف"),
            UploadedByUserName = law.UploadedByUser?.FullName,
            RejectionReason = law.RejectionReason, ApprovedAt = law.ApprovedAt
        };
    }
}