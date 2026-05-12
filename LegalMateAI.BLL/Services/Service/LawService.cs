// LegalMateAI.BLL/Services/Service/LawService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
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
        }

        // ========== للجميع ==========

        public async Task<List<LawDto>> GetLawsAsync(LawCategory? category = null, string? search = null)
        {
            var query = _context.Laws
                .Include(l => l.AddedByAdmin)
                .Include(l => l.UploadedByUser)
                .Where(l => l.IsActive && l.IsApproved)
                .AsQueryable();

            if (category.HasValue)
                query = query.Where(l => l.Category == category.Value);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(l =>
                    l.Name.ToLower().Contains(search) ||
                    (l.Description != null && l.Description.ToLower().Contains(search)) ||
                    (l.SearchKeywords != null && l.SearchKeywords.ToLower().Contains(search)) ||
                    (l.LawNumber != null && l.LawNumber.ToLower().Contains(search)));
            }

            var laws = await query
                .OrderBy(l => l.Category)
                .ThenBy(l => l.Name)
                .ToListAsync();

            foreach (var law in laws) law.ViewCount++;
            await _context.SaveChangesAsync();

            return laws.Select(MapToDto).ToList();
        }

        public async Task<List<LawDto>> SearchLawsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return new List<LawDto>();
            searchTerm = searchTerm.ToLower().Trim();
            var searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var laws = await _context.Laws
                .Include(l => l.AddedByAdmin)
                .Include(l => l.UploadedByUser)
                .Where(l => l.IsActive && l.IsApproved)
                .ToListAsync();

            return laws
                .Select(l => new { Law = l, Score = CalculateMatchScore(l, searchTerm, searchWords) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => MapToDto(x.Law))
                .ToList();
        }

        public async Task<LawDto?> GetLawByIdAsync(Guid id)
        {
            var law = await _context.Laws
                .Include(l => l.AddedByAdmin)
                .Include(l => l.UploadedByUser)
                .FirstOrDefaultAsync(l => l.Id == id && l.IsActive && l.IsApproved);

            if (law == null) return null;
            law.ViewCount++;
            await _context.SaveChangesAsync();
            return MapToDto(law);
        }

        public async Task<byte[]?> DownloadLawAsync(Guid id)
        {
            var law = await _context.Laws.FindAsync(id);
            if (law == null || !law.IsActive || !law.IsApproved) return null;

            law.DownloadCount++;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(law.PdfFileUrl) && law.PdfFileUrl.StartsWith("/uploads/"))
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(webRoot, law.PdfFileUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (File.Exists(filePath)) return await File.ReadAllBytesAsync(filePath);
            }
            return null;
        }

        public async Task<string?> GetLawDownloadUrlAsync(Guid id)
        {
            var law = await _context.Laws.FindAsync(id);
            if (law == null || !law.IsActive || !law.IsApproved) return null;

            law.DownloadCount++;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(law.PdfFileUrl)) return law.PdfFileUrl;
            if (!string.IsNullOrEmpty(law.SourceUrl)) return law.SourceUrl;

            return null;
        }

        public async Task<List<LawCategoryDto>> GetLawCategoriesAsync()
        {
            var categories = await _context.Laws
                .Where(l => l.IsActive && l.IsApproved)
                .Select(l => l.Category)
                .Distinct()
                .ToListAsync();

            return categories.Select(c => new LawCategoryDto
            {
                Category = c,
                Name = GetCategoryName(c),
                Count = _context.Laws.Count(l => l.Category == c && l.IsActive && l.IsApproved)
            }).OrderBy(c => c.Name).ToList();
        }

        // ========== رفع القوانين (للمستخدمين المسجلين فقط) ==========

        public async Task<LawDto?> UploadLawByUserAsync(Guid? userId, AddLawDto request)
        {
            if (!userId.HasValue) return null;

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return null;

            if (!request.PdfFile.ContentType.Contains("pdf")) return null;

            var folderName = GetFolderNameByCategory(request.Category);
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var lawsFolder = Path.Combine(webRootPath, "uploads", "laws", folderName);

            if (!Directory.Exists(lawsFolder)) Directory.CreateDirectory(lawsFolder);

            var safeName = GenerateSafeFileName(request.Name, request.LawNumber, request.Year);
            var filePath = Path.Combine(lawsFolder, safeName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await request.PdfFile.CopyToAsync(stream);

            var law = new Law
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                LawNumber = request.LawNumber,
                Year = request.Year,
                Category = request.Category,
                Description = request.Description,
                PdfFileUrl = $"/uploads/laws/{folderName}/{safeName}",
                SourceUrl = request.SourceUrl,
                SearchKeywords = request.SearchKeywords,
                IsActive = false,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow,
                UploadedByUserId = userId.Value
            };

            _context.Laws.Add(law);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Law uploaded: {law.Name} (Pending Approval)");
            return MapToDto(law);
        }

        public async Task<List<LawDto>> GetUserUploadedLawsAsync(Guid userId)
        {
            var laws = await _context.Laws
                .Include(l => l.AddedByAdmin)
                .Include(l => l.UploadedByUser)
                .Where(l => l.UploadedByUserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return laws.Select(MapToDto).ToList();
        }

        // ========== للأدمن فقط ==========

        public async Task<List<LawDto>> GetPendingLawsAsync()
        {
            var laws = await _context.Laws
                .Include(l => l.AddedByAdmin)
                .Include(l => l.UploadedByUser)
                .Where(l => !l.IsApproved)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return laws.Select(MapToDto).ToList();
        }

        public async Task<bool> ApproveLawAsync(Guid adminId, Guid lawId)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;

            law.IsApproved = true;
            law.IsActive = true;
            law.ApprovedByAdminId = adminId;
            law.ApprovedAt = DateTime.UtcNow;
            law.RejectionReason = null;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Law approved by admin {adminId}: {lawId}");
            return true;
        }

        /// <summary>
        /// ✅ رفض قانون - حذف نهائي من النظام
        /// </summary>
        public async Task<bool> RejectLawAsync(Guid adminId, Guid lawId, string reason)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;

            _logger.LogInformation($"Law rejected and deleted by admin {adminId}: {lawId}, Reason: {reason}");

            // حذف الملف الفعلي لو موجود
            if (!string.IsNullOrEmpty(law.PdfFileUrl) && law.PdfFileUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", law.PdfFileUrl.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }

            // حذف القانون من الداتابيز
            _context.Laws.Remove(law);
            await _context.SaveChangesAsync();

            return true;
        }

        // ========== Helper Methods ==========

        private int CalculateMatchScore(Law law, string searchTerm, string[] searchWords)
        {
            int score = 0;
            var nameLower = law.Name.ToLower();
            var descLower = law.Description?.ToLower() ?? "";
            var keywordsLower = law.SearchKeywords?.ToLower() ?? "";
            var lawNumberLower = law.LawNumber?.ToLower() ?? "";

            if (nameLower.Contains(searchTerm)) score += 50;
            if (descLower.Contains(searchTerm)) score += 20;
            if (keywordsLower.Contains(searchTerm)) score += 30;
            if (lawNumberLower.Contains(searchTerm)) score += 40;

            foreach (var word in searchWords)
            {
                if (nameLower.Contains(word)) score += 10;
                if (keywordsLower.Contains(word)) score += 5;
            }
            return score;
        }

        private string GenerateSafeFileName(string lawName, string? lawNumber, int? year)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(lawName.Where(c => !invalidChars.Contains(c)).ToArray());
            safeName = safeName.Replace(" ", "_");

            if (!string.IsNullOrEmpty(lawNumber)) safeName = $"{safeName}_رقم_{lawNumber}";
            if (year.HasValue) safeName = $"{safeName}_لسنة_{year}";
            if (safeName.Length > 100) safeName = safeName[..100];

            return $"{safeName}.pdf";
        }

        private string GetFolderNameByCategory(LawCategory category) => category switch
        {
            LawCategory.Constitutional => "constitutional",
            LawCategory.Civil => "civil",
            LawCategory.Commercial => "commercial",
            LawCategory.Criminal => "criminal",
            LawCategory.Family => "family",
            LawCategory.Labor => "labor",
            LawCategory.Tax => "tax",
            LawCategory.Administrative => "administrative",
            LawCategory.RealEstate => "real_estate",
            LawCategory.Investment => "investment",
            _ => "other"
        };

        private string GetCategoryName(LawCategory category) => category switch
        {
            LawCategory.Constitutional => "دستوري",
            LawCategory.Civil => "مدني",
            LawCategory.Commercial => "تجاري",
            LawCategory.Criminal => "جنائي",
            LawCategory.Family => "أحوال شخصية",
            LawCategory.Labor => "عمل",
            LawCategory.Tax => "ضريبي",
            LawCategory.Administrative => "إداري",
            LawCategory.RealEstate => "عقاري",
            LawCategory.Investment => "استثمار",
            LawCategory.Maritime => "بحري",
            _ => category.ToString()
        };

        private LawDto MapToDto(Law law)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";

            return new LawDto
            {
                Id = law.Id,
                Name = law.Name,
                LawNumber = law.LawNumber,
                Year = law.Year,
                Category = law.Category,
                CategoryName = GetCategoryName(law.Category),
                Description = law.Description,
                PdfFileUrl = law.PdfFileUrl?.StartsWith("/uploads/") == true ? $"{baseUrl}{law.PdfFileUrl}" : law.PdfFileUrl,
                SourceUrl = law.SourceUrl,
                SearchKeywords = law.SearchKeywords?.Split(',').Select(k => k.Trim()).ToList() ?? new(),
                DownloadCount = law.DownloadCount,
                ViewCount = law.ViewCount,
                IsActive = law.IsActive,
                IsApproved = law.IsApproved,
                CreatedAt = law.CreatedAt,
                AddedByAdminName = law.AddedByAdmin?.FullName ?? (law.UploadedByUser?.FullName ?? "غير معروف"),
                UploadedByUserName = law.UploadedByUser?.FullName,
                RejectionReason = law.RejectionReason,
                ApprovedAt = law.ApprovedAt
            };
        }
    }
}