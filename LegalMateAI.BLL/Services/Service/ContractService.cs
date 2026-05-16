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
        // 3. GET PLACEHOLDERS
        // =========================================================
        public async Task<List<string>> GetTemplatePlaceholdersAsync(Guid templateId)
        {
            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);

            if (template == null || string.IsNullOrEmpty(template.TemplateFilePath))
                return new List<string>();

            var webRootPath = _env.WebRootPath ??
                              Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var templatePath = Path.Combine(
                webRootPath,
                template.TemplateFilePath.TrimStart('/'));

            if (!File.Exists(templatePath))
                return new List<string>();

            var placeholders = new List<string>();

            try
            {
                using var doc = DocX.Load(templatePath);
                var regex = new Regex(@"\{\{([^}]+)\}\}");
                var matches = regex.Matches(doc.Text);

                foreach (Match match in matches)
                {
                    var placeholder = match.Groups[1].Value.Trim();
                    if (!placeholders.Contains(placeholder))
                        placeholders.Add(placeholder);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read placeholders from template {TemplateId}", templateId);
            }

            return placeholders;
        }

        // =========================================================
        // 4. ANALYZE TEMPLATE
        // =========================================================
        public async Task<TemplateAnalysisDto> AnalyzeTemplateFullAsync(Guid templateId)
        {
            var result = new TemplateAnalysisDto();

            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);

            if (template == null || string.IsNullOrEmpty(template.TemplateFilePath))
            {
                result.Message = "القالب غير موجود";
                return result;
            }

            var webRootPath = _env.WebRootPath ??
                              Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var templatePath = Path.Combine(
                webRootPath,
                template.TemplateFilePath.TrimStart('/'));

            if (!File.Exists(templatePath))
            {
                result.Message = "ملف القالب غير موجود";
                return result;
            }

            result.TemplateId = templateId;
            result.TemplateName = template.Name;

            try
            {
                using var doc = DocX.Load(templatePath);
                var fullText = doc.Text;
                var placeholders = new List<PlaceholderFieldDto>();
                int order = 1;

                var regex = new Regex(@"\{\{([^}]+)\}\}");
                var matches = regex.Matches(fullText);

                foreach (Match match in matches)
                {
                    var originalName = match.Groups[1].Value.Trim();
                    
                    placeholders.Add(new PlaceholderFieldDto
                    {
                        Name = originalName,
                        Type = "text",
                        Label = originalName.Replace("_", " "),
                        IsRequired = true,
                        Order = order,
                        Placeholder = "أدخل البيانات"
                    });
                    order++;
                }

                result.Placeholders = placeholders.OrderBy(p => p.Order).ToList();
                result.PlaceholdersCount = placeholders.Count;
                result.UniqueFields = new List<UniqueFieldDto>();
                result.SignatureFields = new List<string>();

                if (placeholders.Count == 0)
                {
                    result.Message = "لا توجد Placeholders في هذا القالب";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze template");
                result.Message = ex.Message;
            }

            return result;
        }

        // =========================================================
        // 5. GENERATE CONTRACT
        // =========================================================
        public async Task<ContractResponseDto?> GenerateContractFromTemplateAsync(
            Guid userId,
            GenerateContractRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive);

            if (template == null) return null;

            if (string.IsNullOrEmpty(template.TemplateFilePath))
            {
                _logger.LogError("Template {TemplateId} has no file path", template.Id);
                return null;
            }

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            
            var uploadsFolder = Path.Combine(
                webRootPath,
                "uploads",
                "contracts",
                "user",
                userId.ToString());

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}.docx";
            var fullPath = Path.Combine(uploadsFolder, fileName);
            var templatePath = Path.Combine(webRootPath, template.TemplateFilePath.TrimStart('/'));

            if (!File.Exists(templatePath))
            {
                _logger.LogError("Template file not found: {TemplatePath}", templatePath);
                return null;
            }

            try
            {
                File.Copy(templatePath, fullPath, true);

                using var doc = DocX.Load(fullPath);
                var replacedCount = 0;

                foreach (var item in request.FilledData)
                {
                    var placeholder = $"{{{{{item.Key}}}}}";
                    var value = item.Value?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(value))
                    {
                        doc.ReplaceText(placeholder, value);
                        replacedCount++;
                    }
                }

                if (!string.IsNullOrEmpty(request.ContractTitle))
                {
                    doc.ReplaceText("{{ContractTitle}}", request.ContractTitle);
                }

                doc.Save();
                _logger.LogInformation("Replaced {Count} placeholders in contract", replacedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed generating contract");
                return null;
            }

            var contract = new Contract
            {
                Id = Guid.NewGuid(),
                ContractNumber = GenerateContractNumber(),
                UserId = userId,
                LawyerId = request.LawyerId,
                Title = request.ContractTitle ?? template.Name,
                Type = template.Type,
                Content = JsonSerializer.Serialize(request.FilledData),
                FileUrl = $"/uploads/contracts/user/{userId}/{fileName}",
                FileFormat = "docx",
                Status = ContractStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            return MapToDto(contract);
        }

        // =========================================================
        // 6. ADD PLACEHOLDERS TO ALL TEMPLATES (ADMIN ONLY)
        // =========================================================
        public async Task<int> AddPlaceholdersToAllTemplatesAsync()
        {
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var templatesFolder = Path.Combine(webRootPath, "uploads", "contracts", "templates");
            
            if (!Directory.Exists(templatesFolder))
            {
                _logger.LogWarning("Templates folder not found: {Folder}", templatesFolder);
                return 0;
            }
            
            var allTemplates = await _context.ContractTemplates.ToListAsync();
            var processedCount = 0;
            
            _logger.LogInformation("Starting to add placeholders to {Count} templates", allTemplates.Count);
            
            foreach (var template in allTemplates)
            {
                if (string.IsNullOrEmpty(template.TemplateFilePath))
                {
                    _logger.LogWarning("Template {TemplateName} has no file path", template.Name);
                    continue;
                }
                
                var filePath = Path.Combine(webRootPath, template.TemplateFilePath.TrimStart('/'));
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Template file not found: {FilePath}", filePath);
                    continue;
                }
                
                try
                {
                    // إنشاء نسخة احتياطية
                    var backupPath = filePath + ".backup";
                    if (!File.Exists(backupPath))
                    {
                        File.Copy(filePath, backupPath);
                        _logger.LogInformation("Created backup: {BackupPath}", backupPath);
                    }
                    
                    using var doc = DocX.Load(filePath);
                    var text = doc.Text;
                    var counter = 1;
                    var hasChanges = false;
                    var emptyPatterns = new[] { @"\.{4,}", @"_{3,}", @"\(\s*\)", @"\(\s*\.{2,}\s*\)", @"\[.*?\]" };
                    
                    // معالجة الفقرات للبحث عن فراغات
                    foreach (var para in doc.Paragraphs)
                    {
                        var paraText = para.Text;
                        var newText = paraText;
                        var modifications = new List<(int index, int length, string placeholder)>();
                        
                        foreach (var pattern in emptyPatterns)
                        {
                            var regex = new Regex(pattern);
                            var matches = regex.Matches(paraText);
                            
                            foreach (Match match in matches.Cast<Match>().Reverse())
                            {
                                var placeholder = $"{{{{field_{counter}}}}}";
                                modifications.Add((match.Index, match.Length, placeholder));
                                counter++;
                                hasChanges = true;
                            }
                        }
                        
                        foreach (var mod in modifications.OrderByDescending(m => m.index))
                        {
                            newText = newText.Substring(0, mod.index) + mod.placeholder + newText.Substring(mod.index + mod.length);
                        }
                        
                        if (newText != paraText)
                        {
                            para.ReplaceText(paraText, newText);
                        }
                    }
                    
                    // معالجة الجداول للبحث عن فراغات
                    // معالجة الجداول للبحث عن فراغات
foreach (var table in doc.Tables)
{
    foreach (var row in table.Rows)
    {
        foreach (var cell in row.Cells)
        {
            var cellText = cell.Paragraphs.FirstOrDefault()?.Text ?? "";
            var newCellText = cellText;
            var modifications = new List<(int index, int length, string placeholder)>();
            
            foreach (var pattern in emptyPatterns)
            {
                var regex = new Regex(pattern);
                var matches = regex.Matches(cellText);
                
                foreach (Match match in matches.Cast<Match>().Reverse())
                {
                    var placeholder = $"{{{{field_{counter}}}}}";
                    modifications.Add((match.Index, match.Length, placeholder));
                    counter++;
                    hasChanges = true;
                }
            }
            
            foreach (var mod in modifications.OrderByDescending(m => m.index))
            {
                newCellText = newCellText.Substring(0, mod.index) + mod.placeholder + newCellText.Substring(mod.index + mod.length);
            }
            
            if (newCellText != cellText)
            {
                foreach (var para in cell.Paragraphs)
                {
                    para.ReplaceText(cellText, newCellText);
                }
            }
        }
    }
}
                    
                    // لو مفيش فراغات، أضف صفحة جديدة في البداية فيها Placeholders
                    if (!hasChanges)
                    {
                        var firstParagraph = doc.InsertParagraph();
                        firstParagraph.InsertText("=== بيانات العقد ===\n");
                        firstParagraph.InsertText($"اسم المشروع/الشركة: {{{{field_1}}}}\n");
                        firstParagraph.InsertText($"الطرف الثاني/المورد: {{{{field_2}}}}\n");
                        firstParagraph.InsertText($"العنوان: {{{{field_3}}}}\n");
                        firstParagraph.InsertText($"المبلغ: {{{{field_4}}}}\n");
                        firstParagraph.InsertText($"التاريخ: {{{{field_5}}}}\n");
                        firstParagraph.InsertText($"الرقم القومي: {{{{field_6}}}}\n");
                        firstParagraph.InsertText($"رقم الهاتف: {{{{field_7}}}}\n");
                        firstParagraph.InsertText($"البريد الإلكتروني: {{{{field_8}}}}\n");
                        firstParagraph.InsertText($"التوقيع: {{{{field_9}}}}\n");
                        
                        hasChanges = true;
                        counter = 10;
                        _logger.LogInformation("Added default placeholders to: {TemplateName}", template.Name);
                    }
                    
                    if (hasChanges)
                    {
                        doc.Save();
                        processedCount++;
                        _logger.LogInformation("✅ Added placeholders to: {TemplateName} (Total: {Count} placeholders)", template.Name, counter - 1);
                    }
                    else
                    {
                        _logger.LogInformation("No changes made to: {TemplateName}", template.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process template {TemplateName}", template.Name);
                }
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Completed. Processed {Count} templates", processedCount);
            return processedCount;
        }

        // =========================================================
        // 7. CONVERT DOC TO DOCX (ADMIN ONLY)
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
        // 8. EXTRACT AND REPLACE EMPTY SPACES
        // =========================================================
        public async Task<List<string>> ExtractAndReplaceEmptySpacesAsync(Guid templateId)
        {
            var template = await _context.ContractTemplates.FindAsync(templateId);
            if (template == null || string.IsNullOrEmpty(template.TemplateFilePath))
                return new List<string>();

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var templatePath = Path.Combine(webRootPath, template.TemplateFilePath.TrimStart('/'));

            if (!File.Exists(templatePath))
                return new List<string>();

            var placeholders = new List<string>();
            var counter = 1;

            try
            {
                using var doc = DocX.Load(templatePath);
                var emptyPatterns = new[] { @"\.{4,}", @"_{3,}", @"\(\s*\)", @"\(\s*\.{2,}\s*\)", @"\[.*?\]" };

                foreach (var para in doc.Paragraphs)
                {
                    var paraText = para.Text;
                    var newText = paraText;
                    var modifications = new List<(int index, int length, string placeholder)>();

                    foreach (var pattern in emptyPatterns)
                    {
                        var regex = new Regex(pattern);
                        var matches = regex.Matches(paraText);

                        foreach (Match match in matches.Cast<Match>().Reverse())
                        {
                            var placeholder = $"{{{{field_{counter}}}}}";
                            modifications.Add((match.Index, match.Length, placeholder));
                            placeholders.Add($"field_{counter}");
                            counter++;
                        }
                    }

                    foreach (var mod in modifications.OrderByDescending(m => m.index))
                    {
                        newText = newText.Substring(0, mod.index) + mod.placeholder + newText.Substring(mod.index + mod.length);
                    }

                    if (newText != paraText)
                    {
                        para.ReplaceText(paraText, newText);
                    }
                }

                doc.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract placeholders from template {TemplateId}", templateId);
                return new List<string>();
            }

            return placeholders;
        }

        // =========================================================
        // HELPER METHODS
        // =========================================================

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

    public class FieldRule
    {
        public string Pattern { get; }
        public string FieldName { get; }
        public string FieldType { get; }
        public string? RegexPattern { get; }
        public string Placeholder { get; }

        public FieldRule(string pattern, string fieldName, string fieldType, string? regexPattern, string placeholder)
        {
            Pattern = pattern;
            FieldName = fieldName;
            FieldType = fieldType;
            RegexPattern = regexPattern;
            Placeholder = placeholder;
        }
    }

    public class FieldMetadata
    {
        public string FieldId { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string Label { get; set; } = string.Empty;
        public string? RegexPattern { get; set; }
        public string? Placeholder { get; set; }
        public int Order { get; set; }
    }
}