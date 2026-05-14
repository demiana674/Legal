using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using System.Text.RegularExpressions;

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

        // ==================== Helper Methods ====================

        private string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            return $"{request?.Scheme}://{request?.Host}";
        }

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
            LawCategory.International => "دولي",
            LawCategory.Educational => "تعليمي",
            LawCategory.Economic => "اقتصادي",
            LawCategory.Financial => "مالي",
            LawCategory.Procedure => "إجرائي",
            LawCategory.Social => "اجتماعي",
            LawCategory.Other => "أخرى",
            _ => category.ToString()
        };

        private string GenerateSafeFileName(string lawName, string? lawNumber, int? year)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(lawName.Where(c => !invalidChars.Contains(c)).ToArray());
            safeName = Regex.Replace(safeName, @"\s+", "_");

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

        private async Task<string?> SavePdfFileAsync(IFormFile? pdfFile, LawCategory category, string lawName, string? lawNumber, int? year)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                return null;

            if (!pdfFile.ContentType.Contains("pdf"))
                return null;

            var folderName = GetFolderNameByCategory(category);
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var lawsFolder = Path.Combine(webRootPath, "uploads", "laws", folderName);

            if (!Directory.Exists(lawsFolder))
                Directory.CreateDirectory(lawsFolder);

            var safeName = GenerateSafeFileName(lawName, lawNumber, year);
            var filePath = Path.Combine(lawsFolder, safeName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await pdfFile.CopyToAsync(stream);

            return $"/uploads/laws/{folderName}/{safeName}";
        }

        private async Task DeletePdfFileAsync(string? pdfFileUrl)
        {
            if (string.IsNullOrEmpty(pdfFileUrl) || !pdfFileUrl.StartsWith("/uploads/"))
                return;

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, pdfFileUrl.TrimStart('/'));

            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        // ==================== للجميع ====================

        public async Task<List<LawCoreInfoDto>> GetAllLawsAsync(LawCategory? category = null, string? search = null)
        {
            var query = _context.Laws
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

            return laws.Select(l => new LawCoreInfoDto
            {
                Id = l.Id,
                Name = l.Name,
                LawNumber = l.LawNumber,
                Year = l.Year,
                Category = l.Category,
                CategoryName = GetCategoryName(l.Category),
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        public async Task<List<LawCoreInfoDto>> SearchLawsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<LawCoreInfoDto>();

            searchTerm = searchTerm.ToLower().Trim();
            var searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var laws = await _context.Laws
                .Where(l => l.IsActive && l.IsApproved)
                .ToListAsync();

            return laws
                .Select(l => new { Law = l, Score = CalculateMatchScore(l, searchTerm, searchWords) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => new LawCoreInfoDto
                {
                    Id = x.Law.Id,
                    Name = x.Law.Name,
                    LawNumber = x.Law.LawNumber,
                    Year = x.Law.Year,
                    Category = x.Law.Category,
                    CategoryName = GetCategoryName(x.Law.Category),
                    CreatedAt = x.Law.CreatedAt
                })
                .ToList();
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

        public async Task<LawCoreInfoDto?> GetLawByIdAsync(Guid id)
        {
            var law = await _context.Laws
                .FirstOrDefaultAsync(l => l.Id == id && l.IsActive && l.IsApproved);

            if (law == null) return null;

            law.ViewCount++;
            await _context.SaveChangesAsync();

            return new LawCoreInfoDto
            {
                Id = law.Id,
                Name = law.Name,
                LawNumber = law.LawNumber,
                Year = law.Year,
                Category = law.Category,
                CategoryName = GetCategoryName(law.Category),
                CreatedAt = law.CreatedAt
            };
        }

        public async Task<byte[]?> DownloadLawAsync(Guid id)
        {
            var law = await _context.Laws.FindAsync(id);
            if (law == null || !law.IsActive || !law.IsApproved) return null;

            law.DownloadCount++;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(law.PdfFileUrl) && law.PdfFileUrl.StartsWith("/uploads/"))
            {
                var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(webRootPath, law.PdfFileUrl.TrimStart('/'));
                if (File.Exists(filePath))
                    return await File.ReadAllBytesAsync(filePath);
            }
            return null;
        }

        public async Task<string?> GetLawDownloadUrlAsync(Guid id)
        {
            var law = await _context.Laws.FindAsync(id);
            if (law == null || !law.IsActive || !law.IsApproved) return null;

            law.DownloadCount++;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(law.PdfFileUrl))
                return law.PdfFileUrl;
            if (!string.IsNullOrEmpty(law.SourceUrl))
                return law.SourceUrl;

            return null;
        }

        public async Task<List<LawCategoryDto>> GetLawCategoriesAsync()
        {
            var categories = await _context.Laws
                .Where(l => l.IsActive && l.IsApproved)
                .GroupBy(l => l.Category)
                .Select(g => new LawCategoryDto
                {
                    Category = g.Key,
                    Name = GetCategoryName(g.Key),
                    Count = g.Count()
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return categories;
        }

        // ==================== للمستخدمين المسجلين ====================

        public async Task<LawCoreInfoDto?> UploadLawByUserAsync(Guid? userId, AddLawDto request)
        {
            if (!userId.HasValue) return null;

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return null;

            if (request.PdfFile == null || !request.PdfFile.ContentType.Contains("pdf"))
                return null;

            var pdfPath = await SavePdfFileAsync(request.PdfFile, request.Category, request.Name, request.LawNumber, request.Year);

            var law = new Law
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                LawNumber = request.LawNumber,
                Year = request.Year,
                Category = request.Category,
                Description = request.Description,
                PdfFileUrl = pdfPath,
                SourceUrl = request.SourceUrl,
                SearchKeywords = request.SearchKeywords,
                IsActive = false,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow,
                UploadedByUserId = userId.Value
            };

            _context.Laws.Add(law);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Law uploaded by user {userId}: {law.Name} (Pending Approval)");

            return new LawCoreInfoDto
            {
                Id = law.Id,
                Name = law.Name,
                LawNumber = law.LawNumber,
                Year = law.Year,
                Category = law.Category,
                CategoryName = GetCategoryName(law.Category),
                CreatedAt = law.CreatedAt
            };
        }

        public async Task<List<LawCoreInfoDto>> GetUserUploadedLawsAsync(Guid userId)
        {
            var laws = await _context.Laws
                .Where(l => l.UploadedByUserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return laws.Select(l => new LawCoreInfoDto
            {
                Id = l.Id,
                Name = l.Name,
                LawNumber = l.LawNumber,
                Year = l.Year,
                Category = l.Category,
                CategoryName = GetCategoryName(l.Category),
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        // ==================== للأدمن فقط ====================

        public async Task<LawCoreInfoDto?> CreateLawAsync(Guid adminId, CreateLawDto request)
        {
            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null) return null;

            string? pdfPath = null;
            if (request.PdfFile != null)
            {
                pdfPath = await SavePdfFileAsync(request.PdfFile, request.Category, request.Name, request.LawNumber, request.Year);
            }
            else if (!string.IsNullOrEmpty(request.PdfFileUrl))
            {
                pdfPath = request.PdfFileUrl;
            }

            var law = new Law
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                LawNumber = request.LawNumber,
                Year = request.Year,
                Category = request.Category,
                Description = request.Description,
                PdfFileUrl = pdfPath,
                SourceUrl = request.SourceUrl,
                SearchKeywords = request.SearchKeywords,
                IsActive = true,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow,
                AddedByAdminId = adminId
            };

            _context.Laws.Add(law);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Law created by admin {adminId}: {law.Name}");

            return new LawCoreInfoDto
            {
                Id = law.Id,
                Name = law.Name,
                LawNumber = law.LawNumber,
                Year = law.Year,
                Category = law.Category,
                CategoryName = GetCategoryName(law.Category),
                CreatedAt = law.CreatedAt
            };
        }

        public async Task<LawCoreInfoDto?> UpdateLawAsync(Guid adminId, Guid lawId, UpdateLawDto request)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return null;

            bool hasChanges = false;

            if (!string.IsNullOrEmpty(request.Name))
            {
                law.Name = request.Name;
                hasChanges = true;
            }
            if (request.LawNumber != null)
            {
                law.LawNumber = request.LawNumber;
                hasChanges = true;
            }
            if (request.Year.HasValue)
            {
                law.Year = request.Year;
                hasChanges = true;
            }
            if (request.Category.HasValue)
            {
                law.Category = request.Category.Value;
                hasChanges = true;
            }
            if (request.Description != null)
            {
                law.Description = request.Description;
                hasChanges = true;
            }
            if (request.SourceUrl != null)
            {
                law.SourceUrl = request.SourceUrl;
                hasChanges = true;
            }
            if (request.SearchKeywords != null)
            {
                law.SearchKeywords = request.SearchKeywords;
                hasChanges = true;
            }
            if (request.IsActive.HasValue)
            {
                law.IsActive = request.IsActive.Value;
                hasChanges = true;
            }
            if (request.IsApproved.HasValue)
            {
                law.IsApproved = request.IsApproved.Value;
                hasChanges = true;
            }

            if (request.PdfFile != null)
            {
                await DeletePdfFileAsync(law.PdfFileUrl);
                var newPdfPath = await SavePdfFileAsync(request.PdfFile, law.Category, law.Name, law.LawNumber, law.Year);
                if (newPdfPath != null)
                {
                    law.PdfFileUrl = newPdfPath;
                    hasChanges = true;
                }
            }
            else if (request.PdfFileUrl != null)
            {
                law.PdfFileUrl = request.PdfFileUrl;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Law updated by admin {adminId}: {law.Id}");
            }

            return new LawCoreInfoDto
            {
                Id = law.Id,
                Name = law.Name,
                LawNumber = law.LawNumber,
                Year = law.Year,
                Category = law.Category,
                CategoryName = GetCategoryName(law.Category),
                CreatedAt = law.CreatedAt
            };
        }

        public async Task<bool> DeleteLawAsync(Guid adminId, Guid lawId)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;

            await DeletePdfFileAsync(law.PdfFileUrl);

            _context.Laws.Remove(law);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Law deleted by admin {adminId}: {lawId}");
            return true;
        }

        public async Task<List<LawCoreInfoDto>> GetPendingLawsAsync()
        {
            var laws = await _context.Laws
                .Where(l => !l.IsApproved)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return laws.Select(l => new LawCoreInfoDto
            {
                Id = l.Id,
                Name = l.Name,
                LawNumber = l.LawNumber,
                Year = l.Year,
                Category = l.Category,
                CategoryName = GetCategoryName(l.Category),
                CreatedAt = l.CreatedAt
            }).ToList();
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

        public async Task<bool> RejectLawAsync(Guid adminId, Guid lawId, string reason)
        {
            var law = await _context.Laws.FindAsync(lawId);
            if (law == null) return false;

            _logger.LogInformation($"Law rejected by admin {adminId}: {lawId}, Reason: {reason}");

            await DeletePdfFileAsync(law.PdfFileUrl);

            _context.Laws.Remove(law);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}