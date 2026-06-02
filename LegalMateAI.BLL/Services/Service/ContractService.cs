// LegalMateAI.BLL.Services.Service/ContractService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xceed.Words.NET;

namespace LegalMateAI.BLL.Services.Service
{
    public class ContractService : IContractService
    {
        private readonly LegalMateDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ContractService> _logger;

        public ContractService(
            LegalMateDbContext context,
            IWebHostEnvironment env,
            ILogger<ContractService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // =========================================================
        // 1. GET TEMPLATES
        // =========================================================
        public async Task<List<ContractTemplateResponseDto>> GetContractTemplatesAsync(
            ContractType? type = null,
            string? search = null)
        {
            var query = _context.ContractTemplates.Where(t => t.IsActive);

            if (type.HasValue)
                query = query.Where(t => t.Type == type.Value);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(search) ||
                    (t.Description ?? "").ToLower().Contains(search));
            }

            var templates = await query
                .OrderBy(t => t.Type)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return templates.Select(MapTemplateToDto).ToList();
        }

        // =========================================================
        // 2. GET TEMPLATE BY ID
        // =========================================================
        public async Task<ContractTemplateResponseDto?> GetTemplateByIdAsync(Guid templateId)
        {
            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);

            return template != null ? MapTemplateToDto(template) : null;
        }

        // =========================================================
        // 3. استخراج الـ placeholders من القالب تلقائياً
        // =========================================================
        private List<string> ExtractPlaceholdersFromTemplate(string filePath)
        {
            try
            {
                using var doc = DocX.Load(filePath);
                string text = doc.Text;
                var regex = new Regex(@"\{([^}]+)\}");
                var matches = regex.Matches(text);
                var placeholders = matches.Select(m => m.Groups[1].Value).Distinct().ToList();
                _logger.LogInformation("Extracted {Count} placeholders from template: {Placeholders}",
                    placeholders.Count, string.Join(", ", placeholders));
                return placeholders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract placeholders from template");
                return new List<string>();
            }
        }

        // =========================================================
        // 4. استبدال الـ placeholders في القالب
        // =========================================================
        private void ReplacePlaceholdersInDocument(DocX doc, Dictionary<string, string> filledData)
        {
            foreach (var item in filledData)
            {
                var placeholder = $"{{{item.Key}}}";
                var value = item.Value ?? "";
                _logger.LogInformation("Replacing '{Placeholder}' with '{Value}'", placeholder, value);
                doc.ReplaceText(placeholder, value);
            }
        }

        // =========================================================
        // 5. توليد عقد جديد (مع استخراج placeholders تلقائياً)
        // =========================================================
        public async Task<ContractResponseDto?> GenerateContractFromTemplateAsync(
            Guid userId,
            Guid templateId,
            Dictionary<string, string> filledData,
            string? contractTitle = null)
        {
            _logger.LogInformation("========== START GENERATING CONTRACT ==========");
            _logger.LogInformation("User ID: {UserId}", userId);
            _logger.LogInformation("Template ID: {TemplateId}", templateId);
            _logger.LogInformation("Contract Title: {Title}", contractTitle);
            _logger.LogInformation("Filled Data: {Data}", JsonSerializer.Serialize(filledData));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found: {UserId}", userId);
                return null;
            }

            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);

            if (template == null)
            {
                _logger.LogError("Template not found: {TemplateId}", templateId);
                return null;
            }

            if (string.IsNullOrEmpty(template.TemplateFilePath))
            {
                _logger.LogError("Template {TemplateId} has no file path", template.Id);
                return null;
            }

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            _logger.LogInformation("WebRootPath: {WebRootPath}", webRootPath);

            var uploadsFolder = Path.Combine(
                webRootPath,
                "uploads",
                "contracts",
                "user",
                userId.ToString());

            if (!Directory.Exists(uploadsFolder))
            {
                _logger.LogInformation("Creating directory: {UploadsFolder}", uploadsFolder);
                Directory.CreateDirectory(uploadsFolder);
            }

            var finalTitle = !string.IsNullOrEmpty(contractTitle) ? contractTitle : template.Name;
            var safeFileName = GenerateSafeFileName(finalTitle);
            var fileName = $"{safeFileName}_{DateTime.Now:yyyyMMddHHmmss}.docx";
            var fullPath = Path.Combine(uploadsFolder, fileName);
            var templatePath = Path.Combine(webRootPath, template.TemplateFilePath.TrimStart('/'));

            _logger.LogInformation("Template path: {TemplatePath}", templatePath);
            _logger.LogInformation("Output path: {FullPath}", fullPath);

            if (!File.Exists(templatePath))
            {
                _logger.LogError("Template file not found: {TemplatePath}", templatePath);
                return null;
            }

            try
            {
                // نسخ القالب
                File.Copy(templatePath, fullPath, true);
                _logger.LogInformation("Template copied successfully");

                // استخراج الـ placeholders من القالب (للتسجيل فقط)
                var placeholders = ExtractPlaceholdersFromTemplate(templatePath);
                _logger.LogInformation("Found placeholders in template: {Count}", placeholders.Count);

                // استبدال الـ placeholders بالبيانات
                using var doc = DocX.Load(fullPath);
                ReplacePlaceholdersInDocument(doc, filledData);
                doc.Save();

                _logger.LogInformation("Contract generated successfully for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed generating contract for user {UserId}", userId);
                return null;
            }

            // حفظ العقد في قاعدة البيانات
            var contract = new Contract
            {
                Id = Guid.NewGuid(),
                ContractNumber = GenerateContractNumber(),
                UserId = userId,
                Title = finalTitle,
                Type = template.Type,
                Content = JsonSerializer.Serialize(filledData),
                FileUrl = $"/uploads/contracts/user/{userId}/{fileName}",
                FileFormat = "docx",
                Status = ContractStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Contract saved to DB with ID: {ContractId}", contract.Id);
            _logger.LogInformation("========== END GENERATING CONTRACT ==========");

            return MapToDto(contract);
        }

        // =========================================================
        // 6. CONVERT DOC TO DOCX (ADMIN ONLY)
        // =========================================================
        public async Task<int> ConvertAllDocToDocxAsync()
        {
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var templatesFolder = Path.Combine(webRootPath, "uploads", "contracts", "templates");

            if (!Directory.Exists(templatesFolder))
                return 0;

            var docFiles = Directory.GetFiles(templatesFolder, "*.doc");
            var converted = 0;

            foreach (var docFile in docFiles)
            {
                try
                {
                    var docxFile = Path.ChangeExtension(docFile, ".docx");

                    using (var doc = DocX.Load(docFile))
                    {
                        doc.SaveAs(docxFile);
                    }

                    File.Delete(docFile);
                    converted++;

                    var relativePath = $"/uploads/contracts/templates/{Path.GetFileName(docxFile)}";
                    var templates = await _context.ContractTemplates
                        .Where(t => t.TemplateFilePath != null && t.TemplateFilePath.Contains(Path.GetFileNameWithoutExtension(docFile)))
                        .ToListAsync();

                    foreach (var template in templates)
                    {
                        template.TemplateFilePath = relativePath;
                    }
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Converted: {DocFile} to {DocxFile}", docFile, docxFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert {DocFile}", docFile);
                }
            }

            return converted;
        }

        // =========================================================
        // HELPER METHODS
        // =========================================================

        private string GenerateSafeFileName(string title)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(title.Where(c => !invalidChars.Contains(c)).ToArray());
            safeName = safeName.Replace(" ", "_");

            if (safeName.Length > 50)
                safeName = safeName[..50];

            return safeName;
        }

        private string GenerateContractNumber()
        {
            var count = _context.Contracts.Count() + 1;
            return $"CNT-{DateTime.UtcNow.Year}-{count:D6}";
        }

        private ContractResponseDto MapToDto(Contract c) => new()
        {
            Id = c.Id,
            ContractNumber = c.ContractNumber,
            Title = c.Title,
            Type = c.Type,
            Content = c.Content,
            FileUrl = c.FileUrl,
            Status = c.Status,
            ProgressPercentage = c.ProgressPercentage,
            CreatedAt = c.CreatedAt,
            User = new UserBriefDto
            {
                Id = c.User?.UserID ?? Guid.Empty,
                FullName = c.User?.FullName ?? "",
                Email = c.User?.Email ?? ""
            }
        };

        private ContractTemplateResponseDto MapTemplateToDto(ContractTemplate t) => new()
        {
            Id = t.Id,
            Name = t.Name,
            Type = t.Type,
            Description = t.Description ?? "",
            TemplateContent = t.TemplateContent,
            TemplateFilePath = t.TemplateFilePath ?? "",
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt
        };

        // =========================================================
        // USER CONTRACTS METHODS
        // =========================================================

        public async Task<List<ContractResponseDto>> GetUserContractsAsync(
            Guid userId,
            string? status = null,
            string? search = null)
        {
            var query = _context.Contracts
                .Include(c => c.User)
                .Where(c => c.UserId == userId);

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<ContractStatus>(status, true, out var s))
            {
                query = query.Where(c => c.Status == s);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.Title.ToLower().Contains(search) ||
                    c.ContractNumber.ToLower().Contains(search));
            }

            return (await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync())
                .Select(MapToDto)
                .ToList();
        }

        public async Task<List<ContractResponseDto>> SearchAllContractsAsync(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();

            var query = _context.Contracts
                .Include(c => c.User)
                .Where(c =>
                    c.Title.ToLower().Contains(searchTerm) ||
                    c.ContractNumber.ToLower().Contains(searchTerm));

            return (await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync())
                .Select(MapToDto)
                .ToList();
        }

        public async Task<ContractResponseDto?> GetAnyContractByIdAsync(Guid contractId)
        {
            var contract = await _context.Contracts
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            return contract != null ? MapToDto(contract) : null;
        }

        public async Task<ContractResponseDto?> UpdateContractAsync(
            Guid userId,
            Guid contractId,
            UpdateContractDto request)
        {
            var contract = await _context.Contracts
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

            if (contract == null)
                return null;

            if (!string.IsNullOrEmpty(request.Title))
                contract.Title = request.Title;

            if (request.ProgressPercentage.HasValue)
                contract.ProgressPercentage = request.ProgressPercentage.Value;

            contract.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(contract);
        }

        public async Task<bool> UpdateContractStatusAsync(
            Guid userId,
            Guid contractId,
            UpdateContractStatusDto request)
        {
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

            if (contract == null)
                return false;

            contract.Status = request.Status;

            if (request.Status == ContractStatus.Active)
                contract.SignedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteContractAsync(Guid userId, Guid contractId)
        {
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

            if (contract == null)
                return false;

            if (!string.IsNullOrEmpty(contract.FileUrl))
            {
                var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(webRootPath, contract.FileUrl.TrimStart('/'));

                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<byte[]?> DownloadAnyContractAsync(Guid contractId)
        {
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null || string.IsNullOrEmpty(contract.FileUrl))
                return null;

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, contract.FileUrl.TrimStart('/'));

            if (!File.Exists(filePath))
                return null;

            return await File.ReadAllBytesAsync(filePath);
        }
    }
}