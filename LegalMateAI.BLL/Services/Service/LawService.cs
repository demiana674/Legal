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
        private readonly LawParserService _lawParser;
        private readonly HttpClient _httpClient;

        public LawService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            ILogger<LawService> logger,
            LawParserService lawParser)
        {
            _context = context;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _lawParser = lawParser;
            _httpClient = new HttpClient();
        }

        // ========== للجميع (Guest والمستخدمين) ==========

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

            // زود عداد المشاهدة لكل قانون
            foreach (var law in laws)
            {
                law.ViewCount++;
            }
            await _context.SaveChangesAsync();

            return laws.Select(MapToDto).ToList();
        }

        public async Task<List<LawDto>> SearchLawsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<LawDto>();

            searchTerm = searchTerm.ToLower().Trim();
            var searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var query = _context.Laws
                .Include(l => l.AddedByAdmin)
                .Include(l => l.UploadedByUser)
                .Where(l => l.IsActive && l.IsApproved)
                .AsQueryable();

            var laws = await query.ToListAsync();

            var scoredLaws = laws
                .Select(l => new { Law = l, Score = CalculateMatchScore(l, searchTerm, searchWords) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Law)
                .ToList();

            return scoredLaws.Select(MapToDto).ToList();
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

    if (law == null || !law.IsActive || !law.IsApproved)
        return null;

    // زيادة عدد التحميلات
    law.DownloadCount++;
    await _context.SaveChangesAsync();

    // لو الملف داخلي فقط
    if (!string.IsNullOrEmpty(law.PdfFileUrl) &&
        law.PdfFileUrl.StartsWith("/uploads/"))
    {
        try
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var filePath = Path.Combine(
                webRoot,
                law.PdfFileUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (File.Exists(filePath))
                return await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read local PDF file.");
        }
    }

    // لو الملف خارجي سيبه للـ Controller يعمل Redirect
    return null;
}

        /// <summary>
        /// الحصول على رابط تحميل القانون (يرجع الرابط الخارجي)
        /// </summary>
        public async Task<string?> GetLawDownloadUrlAsync(Guid id)
        {
            var law = await _context.Laws.FindAsync(id);
            if (law == null || !law.IsActive || !law.IsApproved) return null;

            law.DownloadCount++;
            await _context.SaveChangesAsync();

            // أرجع رابط الـ PDF الخارجي (لو موجود)
            if (!string.IsNullOrEmpty(law.PdfFileUrl))
                return law.PdfFileUrl;

            // لو مفيش PDF، أرجع رابط المصدر
            if (!string.IsNullOrEmpty(law.SourceUrl))
                return law.SourceUrl;

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

        // ========== للمستخدمين المسجلين ==========

        /// <summary>
        /// رفع قانون من المستخدم مع بارسر آلي للرابط
        /// </summary>
        public async Task<LawDto?> UploadLawByUserWithParserAsync(Guid userId, CreateLawRequestDto request)
        {
            _logger.LogInformation($"User {userId} uploading law from URL: {request.SourceUrl}");

            // 1. استخرج البيانات من الرابط
            var parsedData = await _lawParser.ParseFromUrlAsync(request.SourceUrl);

            // 2. ادمج البيانات المستخرجة مع اللي المستخدم دخله
            parsedData.MergeWithUserInput(request);

            // 3. إنشاء القانون
            var law = new Law
            {
                Id = Guid.NewGuid(),
                Name = parsedData.Name ?? "قانون غير معروف",
                LawNumber = parsedData.LawNumber,
                Year = parsedData.Year,
                Category = parsedData.Category ?? LawCategory.Other,
                Description = parsedData.Description,
                PdfFileUrl = parsedData.PdfUrl,      // رابط PDF من الموقع الأصلي
                SourceUrl = request.SourceUrl,
                SearchKeywords = parsedData.SearchKeywords,
                IsActive = false,       // غير نشط لغاية ما الأدمن يوافق
                IsApproved = false,     // لم تتم الموافقة بعد
                CreatedAt = DateTime.UtcNow,
                UploadedByUserId = userId
            };

            _context.Laws.Add(law);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Law uploaded by user {userId}: {law.Name} (Pending Approval)");

            return await GetLawByIdForAdminAsync(law.Id);
        }

        /// <summary>
        /// رفع قانون بالطريقة القديمة (PDF file upload)
        /// </summary>
        public async Task<LawDto?> UploadLawByUserAsync(Guid userId, AddLawDto request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            if (!request.PdfFile.ContentType.Contains("pdf"))
                return null;

            var folderName = GetFolderNameByCategory(request.Category);
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var lawsFolder = Path.Combine(webRootPath, "uploads", "laws", folderName);

            if (!Directory.Exists(lawsFolder))
                Directory.CreateDirectory(lawsFolder);

            var safeName = GenerateSafeFileName(request.Name, request.LawNumber, request.Year);
            var filePath = Path.Combine(lawsFolder, safeName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.PdfFile.CopyToAsync(stream);
            }

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
                UploadedByUserId = userId
            };

            _context.Laws.Add(law);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Law uploaded by user {userId}: {law.Name}");
            return await GetLawByIdForAdminAsync(law.Id);
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

        public async Task<LawDto?> AddLawAsync(Guid adminId, IFormFile pdfFile, string name,
            LawCategory category, string? lawNumber, int? year,
            string? description, string? sourceUrl, string? searchKeywords)
        {
            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null) return null;

            if (!pdfFile.ContentType.Contains("pdf"))
                return null;

            var folderName = GetFolderNameByCategory(category);
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var lawsFolder = Path.Combine(webRootPath, "uploads", "laws", folderName);

            if (!Directory.Exists(lawsFolder))
                Directory.CreateDirectory(lawsFolder);

            var safeName = GenerateSafeFileName(name, lawNumber, year);
            var filePath = Path.Combine(lawsFolder, safeName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(stream);
            }

            var law = new Law
            {
                Id = Guid.NewGuid(),
                Name = name,
                LawNumber = lawNumber,
                Year = year,
                Category = category,
                Description = description,
                PdfFileUrl = $"/uploads/laws/{folderName}/{safeName}",
                SourceUrl = sourceUrl,
                SearchKeywords = searchKeywords,
                IsActive = true,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow,
                AddedByAdminId = adminId,
                ApprovedByAdminId = adminId,
                ApprovedAt = DateTime.UtcNow
            };

            _context.Laws.Add(law);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Law added by admin {adminId}: {law.Name}");
            return await GetLawByIdForAdminAsync(law.Id);
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
            _logger.LogInformation($"Law updated by admin {adminId}: {lawId}");
            return await GetLawByIdForAdminAsync(lawId);
        }

        public async Task<bool> DeleteLawAsync(Guid adminId, Guid lawId)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;

            // لو في ملف محلي، احذفه
            if (!string.IsNullOrEmpty(law.PdfFileUrl) && law.PdfFileUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", law.PdfFileUrl.TrimStart('/'));
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            _context.Laws.Remove(law);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Law deleted by admin {adminId}: {lawId}");
            return true;
        }

        public async Task<List<LawDto>> GetAllLawsForAdminAsync()
        {
            var laws = await _context.Laws
                .Include(l => l.AddedByAdmin)
                .Include(l => l.UploadedByUser)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return laws.Select(MapToDto).ToList();
        }

        public async Task<List<LawDto>> GetPendingLawsAsync()
        {
            var laws = await _context.Laws
                .Include(l => l.UploadedByUser)
                .Where(l => !l.IsApproved && l.UploadedByUserId != null)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return laws.Select(MapToDto).ToList();
        }

        /// <summary>
        /// جلب القوانين المنتظرة للموافقة من المستخدمين
        /// </summary>
        public async Task<List<LawDto>> GetPendingLawsForAdminAsync()
        {
            var laws = await _context.Laws
                .Include(l => l.UploadedByUser)
                .Where(l => !l.IsApproved && l.UploadedByUserId != null)
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
        /// الموافقة على قانون من المستخدم
        /// </summary>
        public async Task<bool> ApproveUserLawAsync(Guid adminId, Guid lawId)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null || law.UploadedByUserId == null) return false;

            law.IsApproved = true;
            law.IsActive = true;
            law.ApprovedByAdminId = adminId;
            law.ApprovedAt = DateTime.UtcNow;
            law.RejectionReason = null;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"User law {lawId} approved by admin {adminId}");
            return true;
        }

        public async Task<bool> RejectLawAsync(Guid adminId, Guid lawId, string reason)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;

            law.IsApproved = false;
            law.IsActive = false;
            law.ApprovedByAdminId = adminId;
            law.ApprovedAt = DateTime.UtcNow;
            law.RejectionReason = reason;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Law rejected by admin {adminId}: {lawId}, Reason: {reason}");
            return true;
        }

        /// <summary>
        /// رفض قانون من المستخدم
        /// </summary>
        public async Task<bool> RejectUserLawAsync(Guid adminId, Guid lawId, string reason)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;

            law.IsApproved = false;
            law.IsActive = false;
            law.ApprovedByAdminId = adminId;
            law.ApprovedAt = DateTime.UtcNow;
            law.RejectionReason = reason;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"User law {lawId} rejected by admin {adminId}: {reason}");
            return true;
        }

        // ========== Helper Methods ==========

        private async Task<LawDto?> GetLawByIdForAdminAsync(Guid id)
        {
            var law = await _context.Laws
                .Include(l => l.AddedByAdmin)
                .Include(l => l.UploadedByUser)
                .FirstOrDefaultAsync(l => l.Id == id);

            return law != null ? MapToDto(law) : null;
        }

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

            if (!string.IsNullOrEmpty(lawNumber))
                safeName = $"{safeName}_رقم_{lawNumber}";
            if (year.HasValue)
                safeName = $"{safeName}_لسنة_{year}";

            if (safeName.Length > 100)
                safeName = safeName.Substring(0, 100);

            return $"{safeName}.pdf";
        }

        private string GetFolderNameByCategory(LawCategory category)
        {
            return category switch
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
                LawCategory.Maritime => "maritime",
                LawCategory.Procedure => "arbitration",
                LawCategory.Financial => "banking_finance",
                LawCategory.Social => "ngo",
                LawCategory.Educational => "education",
                LawCategory.Economic => "competition",
                _ => "other"
            };
        }

        private string GetCategoryName(LawCategory category)
        {
            return category switch
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
        }

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
                PdfFileUrl = law.PdfFileUrl?.StartsWith("/uploads/") == true 
                    ? $"{baseUrl}{law.PdfFileUrl}" 
                    : law.PdfFileUrl,
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