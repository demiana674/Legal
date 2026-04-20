// LegalMateAI.BLL/Services/Service/PredefinedContractService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.BLL.Services.IService;
using System.Text.Json;

namespace LegalMateAI.BLL.Services.Service
{
    public class PredefinedContractService : IPredefinedContractService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PredefinedContractService> _logger;
        private readonly PdfGenerationService _pdfService;

        public PredefinedContractService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PredefinedContractService> logger,
            PdfGenerationService pdfService)
        {
            _context = context;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _pdfService = pdfService;
        }

        // ========== Admin Operations ==========

        public async Task<PredefinedContractTemplateDto?> UploadTemplateAsync(
            Guid adminId, 
            IFormFile file, 
            string name, 
            string? nameEn,
            string? description, 
            ContractType contractType,
            List<string> requiredFields,
            string? searchKeywords = null,
            bool isFeatured = false)
        {
            _logger.LogInformation($"Uploading template: {name} by admin {adminId}");

            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null) return null;

            // تحديد نوع الملف
            var extension = Path.GetExtension(file.FileName).ToLower();
            var fileFormat = extension == ".pdf" ? "pdf" : (extension == ".docx" || extension == ".doc" ? "docx" : null);
            
            if (fileFormat == null)
            {
                _logger.LogWarning($"Invalid file type: {extension}. Only PDF and DOCX are allowed.");
                return null;
            }

            // إنشاء مجلد للقوالب
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var templatesFolder = Path.Combine(webRootPath, "uploads", "contracts", "templates");
            if (!Directory.Exists(templatesFolder))
                Directory.CreateDirectory(templatesFolder);

            // حفظ الملف
            var fileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(templatesFolder, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var template = new PredefinedContractTemplate
            {
                Id = Guid.NewGuid(),
                Name = name,
                NameEn = nameEn,
                Description = description,
                ContractType = contractType,
                FileFormat = fileFormat,
                FilePath = $"/uploads/contracts/templates/{fileName}",
                RequiredFieldsJson = JsonSerializer.Serialize(requiredFields),
                SearchKeywords = searchKeywords,
                IsActive = true,
                IsFeatured = isFeatured,
                DownloadCount = 0,
                UsageCount = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedByAdminId = adminId
            };

            _context.PredefinedContractTemplates.Add(template);
            await _context.SaveChangesAsync();

            await LogAdminActionAsync(adminId, AdminLogAction.Create, "PredefinedContractTemplate", template.Id);

            _logger.LogInformation($"Template uploaded successfully: {template.Id}");
            return await GetTemplateByIdAsync(template.Id);
        }

        public async Task<PredefinedContractTemplateDto?> UpdateTemplateAsync(
            Guid adminId,
            Guid templateId,
            string? name,
            string? nameEn,
            string? description,
            bool? isActive,
            bool? isFeatured,
            List<string>? requiredFields,
            string? searchKeywords)
        {
            var template = await _context.PredefinedContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null) return null;

            bool hasChanges = false;

            if (!string.IsNullOrEmpty(name)) { template.Name = name; hasChanges = true; }
            if (nameEn != null) { template.NameEn = nameEn; hasChanges = true; }
            if (description != null) { template.Description = description; hasChanges = true; }
            if (isActive.HasValue) { template.IsActive = isActive.Value; hasChanges = true; }
            if (isFeatured.HasValue) { template.IsFeatured = isFeatured.Value; hasChanges = true; }
            if (requiredFields != null) { template.RequiredFieldsJson = JsonSerializer.Serialize(requiredFields); hasChanges = true; }
            if (searchKeywords != null) { template.SearchKeywords = searchKeywords; hasChanges = true; }

            if (hasChanges)
            {
                template.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await LogAdminActionAsync(adminId, AdminLogAction.Update, "PredefinedContractTemplate", templateId);
            }

            return await GetTemplateByIdAsync(templateId);
        }

        public async Task<bool> DeleteTemplateAsync(Guid adminId, Guid templateId)
        {
            var template = await _context.PredefinedContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null) return false;

            // حذف الملف الفعلي
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, template.FilePath.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _context.PredefinedContractTemplates.Remove(template);
            await _context.SaveChangesAsync();

            await LogAdminActionAsync(adminId, AdminLogAction.Delete, "PredefinedContractTemplate", templateId);
            return true;
        }

        public async Task<List<PredefinedContractTemplateDto>> GetAllTemplatesForAdminAsync(
            bool includeInactive = true, 
            ContractType? type = null,
            string? searchTerm = null)
        {
            var query = _context.PredefinedContractTemplates
                .Include(t => t.CreatedByAdmin)
                .AsQueryable();

            if (!includeInactive)
                query = query.Where(t => t.IsActive);

            if (type.HasValue)
                query = query.Where(t => t.ContractType == type.Value);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(t => 
                    t.Name.ToLower().Contains(searchTerm) ||
                    (t.NameEn != null && t.NameEn.ToLower().Contains(searchTerm)) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                    (t.SearchKeywords != null && t.SearchKeywords.ToLower().Contains(searchTerm)));
            }

            var templates = await query
                .OrderBy(t => t.ContractType)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return templates.Select(MapToDto).ToList();
        }

        // ========== User Operations ==========

        public async Task<List<PredefinedContractTemplateDto>> GetActiveTemplatesAsync(
            ContractType? type = null, 
            string? searchTerm = null,
            bool featuredOnly = false)
        {
            var query = _context.PredefinedContractTemplates
                .Include(t => t.CreatedByAdmin)
                .Where(t => t.IsActive)
                .AsQueryable();

            if (type.HasValue)
                query = query.Where(t => t.ContractType == type.Value);

            if (featuredOnly)
                query = query.Where(t => t.IsFeatured);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(t => 
                    t.Name.ToLower().Contains(searchTerm) ||
                    (t.NameEn != null && t.NameEn.ToLower().Contains(searchTerm)) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                    (t.SearchKeywords != null && t.SearchKeywords.ToLower().Contains(searchTerm)));
            }

            var templates = await query
                .OrderByDescending(t => t.IsFeatured)
                .ThenByDescending(t => t.UsageCount)
                .ThenBy(t => t.ContractType)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return templates.Select(MapToDto).ToList();
        }

        public async Task<List<PredefinedContractTemplateDto>> GetPopularTemplatesAsync(int count = 5)
        {
            var templates = await _context.PredefinedContractTemplates
                .Include(t => t.CreatedByAdmin)
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.UsageCount)
                .ThenByDescending(t => t.DownloadCount)
                .Take(count)
                .ToListAsync();

            return templates.Select(MapToDto).ToList();
        }

        public async Task<List<PredefinedContractTemplateDto>> GetFeaturedTemplatesAsync()
        {
            var templates = await _context.PredefinedContractTemplates
                .Include(t => t.CreatedByAdmin)
                .Where(t => t.IsActive && t.IsFeatured)
                .OrderBy(t => t.ContractType)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return templates.Select(MapToDto).ToList();
        }

        public async Task<List<PredefinedContractTemplateDto>> SearchTemplatesAsync(string searchTerm, ContractType? type = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveTemplatesAsync(type);

            searchTerm = searchTerm.ToLower().Trim();
            var searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var query = _context.PredefinedContractTemplates
                .Include(t => t.CreatedByAdmin)
                .Where(t => t.IsActive)
                .AsQueryable();

            if (type.HasValue)
                query = query.Where(t => t.ContractType == type.Value);

            // البحث في كل الكلمات
            var templates = await query.ToListAsync();

            var scoredTemplates = templates.Select(t =>
            {
                var score = CalculateMatchScore(t, searchTerm, searchWords);
                var dto = MapToDto(t);
                dto.MatchScore = score;
                return (dto, score);
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.dto.Name)
            .Select(x => x.dto)
            .ToList();

            return scoredTemplates;
        }

        private double CalculateMatchScore(PredefinedContractTemplate template, string searchTerm, string[] searchWords)
        {
            double score = 0;
            var nameLower = template.Name.ToLower();
            var nameEnLower = template.NameEn?.ToLower() ?? "";
            var descLower = template.Description?.ToLower() ?? "";
            var keywordsLower = template.SearchKeywords?.ToLower() ?? "";

            // تطابق كامل
            if (nameLower == searchTerm) score += 100;
            if (nameEnLower == searchTerm) score += 90;

            foreach (var word in searchWords)
            {
                if (nameLower.Contains(word)) score += 20;
                if (nameEnLower.Contains(word)) score += 15;
                if (descLower.Contains(word)) score += 5;
                if (keywordsLower.Contains(word)) score += 10;
            }

            // نوع العقد
            if (descLower.Contains(GetContractTypeArabic(template.ContractType).ToLower())) score += 15;

            return Math.Min(score, 100);
        }

        private string GetContractTypeArabic(ContractType type)
        {
            return type switch
            {
                ContractType.Rental => "إيجار",
                ContractType.Employment => "عمل",
                ContractType.Sale => "بيع",
                ContractType.Service => "خدمات",
                ContractType.Partnership => "شراكة",
                ContractType.PowerOfAttorney => "وكالة",
                ContractType.Settlement => "صلح",
                _ => ""
            };
        }

        public async Task<PredefinedContractTemplateDto?> GetTemplateByIdAsync(Guid templateId)
        {
            var template = await _context.PredefinedContractTemplates
                .Include(t => t.CreatedByAdmin)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            return template != null ? MapToDto(template) : null;
        }

        public async Task<GeneratedContractDto?> GenerateContractFromTemplateAsync(
            Guid userId,
            Guid templateId,
            Dictionary<string, string> filledData,
            Guid? lawyerId = null,
            string outputFormat = "pdf")
        {
            _logger.LogInformation($"User {userId} generating contract from template {templateId}");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var template = await _context.PredefinedContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);

            if (template == null) return null;

            // تحديث عداد الاستخدام
            template.UsageCount++;
            template.DownloadCount++;

            // توليد رقم العقد
            var contractNumber = GenerateContractNumber();

            // إنشاء مجلد للعقود المولدة
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var generatedFolder = Path.Combine(webRootPath, "uploads", "contracts", "generated", userId.ToString());
            if (!Directory.Exists(generatedFolder))
                Directory.CreateDirectory(generatedFolder);

            var outputFileName = $"{contractNumber}_{DateTime.Now:yyyyMMddHHmmss}.{outputFormat}";
            var outputPath = Path.Combine(generatedFolder, outputFileName);

            byte[]? fileBytes = null;

            if (template.FileFormat == "docx" && outputFormat == "docx")
            {
                // Word to Word - تعبئة البيانات في ملف Word
                fileBytes = await FillWordTemplateAsync(template.FilePath, filledData);
            }
            else if (template.FileFormat == "pdf")
            {
                // PDF - تعبئة البيانات في PDF
                var templatePath = Path.Combine(webRootPath, template.FilePath.TrimStart('/'));
                fileBytes = _pdfService.FillPdfForm(templatePath, filledData);
            }
            else
            {
                // Fallback: إنشاء PDF جديد
                fileBytes = _pdfService.GenerateContractPdf(
                    template.Name,
                    contractNumber,
                    FormatContentFromData(template, filledData),
                    DateTime.UtcNow);
            }

            if (fileBytes == null || fileBytes.Length == 0)
            {
                _logger.LogError("Failed to generate contract file");
                return null;
            }

            await File.WriteAllBytesAsync(outputPath, fileBytes);

            // حفظ في قاعدة البيانات
            var generatedContract = new GeneratedContract
            {
                Id = Guid.NewGuid(),
                ContractNumber = contractNumber,
                Title = filledData.GetValueOrDefault("ContractTitle") ?? template.Name,
                TemplateId = templateId,
                UserId = userId,
                LawyerId = lawyerId,
                FilledDataJson = JsonSerializer.Serialize(filledData),
                FinalPdfPath = $"/uploads/contracts/generated/{userId}/{outputFileName}",
                Status = ContractStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            _context.GeneratedContracts.Add(generatedContract);
            await _context.SaveChangesAsync();

            return await GetGeneratedContractByIdAsync(generatedContract.Id);
        }

        private async Task<byte[]?> FillWordTemplateAsync(string templatePath, Dictionary<string, string> data)
        {
            // TODO: تنفيذ تعبئة قوالب Word باستخدام Open XML SDK
            // حالياً: نرجع محتوى HTML بسيط
            var content = FormatContentFromData(null, data);
            return _pdfService.GenerateContractWord("عقد", "GEN-000", content, DateTime.UtcNow);
        }

        private string FormatContentFromData(PredefinedContractTemplate? template, Dictionary<string, string> data)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<h1>{data.GetValueOrDefault("ContractTitle") ?? "عقد"}</h1>");
            sb.AppendLine("<hr/>");
            foreach (var item in data)
            {
                sb.AppendLine($"<p><strong>{item.Key}:</strong> {item.Value}</p>");
            }
            return sb.ToString();
        }

        public async Task<byte[]?> DownloadGeneratedContractAsync(Guid userId, Guid contractId)
        {
            var contract = await _context.GeneratedContracts
                .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

            if (contract == null) return null;

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, contract.FinalPdfPath.TrimStart('/'));
            
            if (!File.Exists(filePath)) return null;

            return await File.ReadAllBytesAsync(filePath);
        }

        public async Task<List<GeneratedContractDto>> GetUserGeneratedContractsAsync(Guid userId)
        {
            var contracts = await _context.GeneratedContracts
                .Include(c => c.Template)
                .Include(c => c.User)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return contracts.Select(MapGeneratedContractToDto).ToList();
        }

        public async Task<bool> DeleteGeneratedContractAsync(Guid userId, Guid contractId)
        {
            var contract = await _context.GeneratedContracts
                .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

            if (contract == null) return false;

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, contract.FinalPdfPath.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _context.GeneratedContracts.Remove(contract);
            await _context.SaveChangesAsync();
            return true;
        }

        // ========== Helper Methods ==========

        private async Task<GeneratedContractDto?> GetGeneratedContractByIdAsync(Guid contractId)
        {
            var contract = await _context.GeneratedContracts
                .Include(c => c.Template)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            return contract != null ? MapGeneratedContractToDto(contract) : null;
        }

        private async Task LogAdminActionAsync(Guid adminId, AdminLogAction action, string targetType, Guid targetId)
        {
            var log = new AdminLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                Timestamp = DateTime.UtcNow
            };
            _context.AdminLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private string GenerateContractNumber()
        {
            var year = DateTime.UtcNow.Year;
            var count = _context.GeneratedContracts.Count() + 1;
            return $"GEN-{year}-{count:D6}";
        }

        private PredefinedContractTemplateDto MapToDto(PredefinedContractTemplate template)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";

            return new PredefinedContractTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                NameEn = template.NameEn,
                Description = template.Description,
                ContractType = template.ContractType,
                FileFormat = template.FileFormat,
                FileUrl = $"{baseUrl}{template.FilePath}",
                ThumbnailUrl = template.ThumbnailPath != null ? $"{baseUrl}{template.ThumbnailPath}" : null,
                RequiredFields = JsonSerializer.Deserialize<List<string>>(template.RequiredFieldsJson) ?? new List<string>(),
                SearchKeywords = template.SearchKeywords?.Split(',').Select(k => k.Trim()).ToList() ?? new List<string>(),
                IsActive = template.IsActive,
                IsFeatured = template.IsFeatured,
                DownloadCount = template.DownloadCount,
                UsageCount = template.UsageCount,
                Rating = template.Rating,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                AdminName = template.CreatedByAdmin?.FullName ?? "غير معروف"
            };
        }

        private GeneratedContractDto MapGeneratedContractToDto(GeneratedContract contract)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";

            return new GeneratedContractDto
            {
                Id = contract.Id,
                ContractNumber = contract.ContractNumber,
                Title = contract.Title,
                TemplateId = contract.TemplateId,
                TemplateName = contract.Template?.Name ?? "غير معروف",
                ContractType = contract.Template?.ContractType ?? ContractType.Other,
                FilledData = JsonSerializer.Deserialize<Dictionary<string, string>>(contract.FilledDataJson) ?? new Dictionary<string, string>(),
                PdfDownloadUrl = $"{baseUrl}/api/contracts/predefined/download/{contract.Id}",
                Status = contract.Status,
                CreatedAt = contract.CreatedAt,
                ExpiresAt = contract.ExpiresAt,
                User = new UserBriefDto
                {
                    Id = contract.User?.UserID ?? Guid.Empty,
                    FullName = contract.User?.FullName ?? "غير معروف",
                    Email = contract.User?.Email ?? "",
                    PhoneNumber = contract.User?.Phone ?? ""
                },
                Lawyer = null
            };
        }
    }
}