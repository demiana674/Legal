// LegalMateAI.BLL/Services/Service/ContractService.cs
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

        // ========== القوالب (للقراءة فقط) ==========

        public async Task<List<ContractTemplateResponseDto>> GetContractTemplatesAsync(ContractType? type = null, string? search = null)
        {
            var query = _context.ContractTemplates.Where(t => t.IsActive);

            if (type.HasValue)
                query = query.Where(t => t.Type == type.Value);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(t => 
                    t.Name.ToLower().Contains(search) ||
                    t.Description.ToLower().Contains(search));
            }

            var templates = await query
                .OrderBy(t => t.Type)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return templates.Select(MapTemplateToDto).ToList();
        }

        public async Task<ContractTemplateResponseDto?> GetTemplateByIdAsync(Guid templateId)
        {
            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);

            return template != null ? MapTemplateToDto(template) : null;
        }

        public async Task<byte[]?> DownloadTemplateAsync(Guid templateId)
        {
            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null || string.IsNullOrEmpty(template.TemplateContent))
                return null;

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, template.TemplateContent.TrimStart('/'));

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Template file not found: {filePath}");
                return null;
            }

            return await File.ReadAllBytesAsync(filePath);
        }

        // ========== عقود المستخدم ==========

        public async Task<ContractResponseDto?> GenerateContractFromTemplateAsync(Guid userId, GenerateContractRequest request)
        {
            _logger.LogInformation($"User {userId} generating contract from template {request.TemplateId}");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive);

            if (template == null)
            {
                _logger.LogWarning($"Template not found: {request.TemplateId}");
                return null;
            }

            // قراءة ملف القالب
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var templatePath = Path.Combine(webRootPath, template.TemplateContent.TrimStart('/'));

            if (!File.Exists(templatePath))
            {
                _logger.LogWarning($"Template file not found: {templatePath}");
                return null;
            }

            // إنشاء مجلد للمستخدم
            var userFolder = Path.Combine(webRootPath, "uploads", "contracts", "user", userId.ToString());
            if (!Directory.Exists(userFolder))
                Directory.CreateDirectory(userFolder);

            // توليد رقم العقد
            var contractNumber = GenerateContractNumber();
            var fileName = $"{contractNumber}_{DateTime.Now:yyyyMMddHHmmss}.docx";
            var filePath = Path.Combine(userFolder, fileName);

            // نسخ القالب وتعبئة البيانات (بسيط - مجرد نسخ)
            File.Copy(templatePath, filePath);

            // TODO: تعبئة البيانات في ملف Word باستخدام Open XML SDK
            // حالياً: بنحفظ البيانات كـ JSON في Content

            var contract = new Contract
            {
                Id = Guid.NewGuid(),
                ContractNumber = contractNumber,
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

            _logger.LogInformation($"Contract generated: {contract.Id}");
            return await GetContractByIdAsync(userId, contract.Id, false);
        }

        public async Task<List<ContractResponseDto>> GetUserContractsAsync(Guid userId, string? status = null, string? search = null)
        {
            var query = _context.Contracts
                .Include(c => c.User)
                .Include(c => c.Lawyer)
                    .ThenInclude(l => l!.User)
                .Where(c => c.UserId == userId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContractStatus>(status, true, out var statusEnum))
                query = query.Where(c => c.Status == statusEnum);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(c => 
                    c.Title.ToLower().Contains(search) ||
                    c.ContractNumber.ToLower().Contains(search));
            }

            var contracts = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return contracts.Select(MapToDto).ToList();
        }

        public async Task<List<ContractResponseDto>> GetLawyerContractsAsync(Guid lawyerId, string? status = null, string? search = null)
        {
            var query = _context.Contracts
                .Include(c => c.User)
                .Include(c => c.Lawyer)
                    .ThenInclude(l => l!.User)
                .Where(c => c.LawyerId == lawyerId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContractStatus>(status, true, out var statusEnum))
                query = query.Where(c => c.Status == statusEnum);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(c => 
                    c.Title.ToLower().Contains(search) ||
                    c.ContractNumber.ToLower().Contains(search));
            }

            var contracts = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return contracts.Select(MapToDto).ToList();
        }

        public async Task<List<ContractResponseDto>> SearchContractsAsync(Guid userId, string searchTerm, bool isLawyer)
        {
            searchTerm = searchTerm.ToLower();
            
            var query = _context.Contracts
                .Include(c => c.User)
                .Include(c => c.Lawyer)
                    .ThenInclude(l => l!.User)
                .Where(c => isLawyer ? c.LawyerId == userId : c.UserId == userId)
                .Where(c => 
                    c.Title.ToLower().Contains(searchTerm) ||
                    c.ContractNumber.ToLower().Contains(searchTerm));

            var contracts = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return contracts.Select(MapToDto).ToList();
        }

        public async Task<ContractResponseDto?> GetContractByIdAsync(Guid userId, Guid contractId, bool isLawyer = false)
        {
            var contract = await _context.Contracts
                .Include(c => c.User)
                .Include(c => c.Lawyer)
                    .ThenInclude(l => l!.User)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null) return null;

            if (!isLawyer && contract.UserId != userId) return null;
            if (isLawyer && contract.LawyerId != userId) return null;

            return MapToDto(contract);
        }

        public async Task<ContractResponseDto?> UpdateContractAsync(Guid userId, Guid contractId, UpdateContractDto request)
        {
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

            if (contract == null) return null;

            if (!string.IsNullOrEmpty(request.Title))
                contract.Title = request.Title;

            if (request.ProgressPercentage.HasValue)
                contract.ProgressPercentage = request.ProgressPercentage.Value;

            contract.LastModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetContractByIdAsync(userId, contractId, false);
        }

        public async Task<bool> UpdateContractStatusAsync(Guid userId, Guid contractId, UpdateContractStatusDto request, bool isLawyer = false)
        {
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null) return false;

            if (!isLawyer && contract.UserId != userId) return false;
            if (isLawyer && contract.LawyerId != userId) return false;

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

            if (contract == null) return false;

            // حذف الملف الفعلي
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

        public async Task<byte[]?> DownloadContractAsync(Guid userId, Guid contractId)
        {
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

            if (contract == null || string.IsNullOrEmpty(contract.FileUrl)) return null;

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, contract.FileUrl.TrimStart('/'));

            if (!File.Exists(filePath)) return null;

            return await File.ReadAllBytesAsync(filePath);
        }

        // ========== Helper Methods ==========

        private string GenerateContractNumber()
        {
            var year = DateTime.UtcNow.Year;
            var count = _context.Contracts.Count() + 1;
            return $"CNT-{year}-{count:D6}";
        }

        private ContractResponseDto MapToDto(Contract contract)
        {
            var user = contract.User;
            var lawyerProfile = contract.Lawyer;
            var lawyerUser = lawyerProfile?.User;

            return new ContractResponseDto
            {
                Id = contract.Id,
                ContractNumber = contract.ContractNumber,
                Title = contract.Title,
                Type = contract.Type,
                Content = contract.Content,
                FileUrl = contract.FileUrl,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Status = contract.Status,
                ProgressPercentage = contract.ProgressPercentage,
                CreatedAt = contract.CreatedAt,
                LastModifiedAt = contract.LastModifiedAt,
                SignedAt = contract.SignedAt,
                IsGeneratedByAI = false,
                User = new UserBriefDto
                {
                    Id = user?.UserID ?? Guid.Empty,
                    FullName = user?.FullName ?? "",
                    Email = user?.Email ?? "",
                    PhoneNumber = user?.Phone ?? ""
                },
                Lawyer = contract.LawyerId.HasValue ? new LawyerBriefDto
                {
                    Id = contract.LawyerId.Value,
                    FullName = lawyerUser?.FullName ?? "",
                    Specialization = "",
                    Rating = 0
                } : null,
                Clauses = new()
            };
        }

        private ContractTemplateResponseDto MapTemplateToDto(ContractTemplate template)
        {
            return new ContractTemplateResponseDto
            {
                Id = template.Id,
                Name = template.Name,
                Type = template.Type,
                Description = template.Description,
                TemplateContent = template.TemplateContent,
                Placeholders = template.Placeholders,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt
            };
        }
    }
}