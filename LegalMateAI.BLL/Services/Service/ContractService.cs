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

        // ========== القوالب ==========

        public async Task<List<ContractTemplateResponseDto>> GetContractTemplatesAsync(ContractType? type = null, string? search = null)
        {
            var query = _context.ContractTemplates.Where(t => t.IsActive);
            if (type.HasValue) query = query.Where(t => t.Type == type.Value);
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(search) || t.Description.ToLower().Contains(search));
            }
            var templates = await query.OrderBy(t => t.Type).ThenBy(t => t.Name).ToListAsync();
            return templates.Select(MapTemplateToDto).ToList();
        }

        public async Task<ContractTemplateResponseDto?> GetTemplateByIdAsync(Guid templateId)
        {
            var template = await _context.ContractTemplates.FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);
            return template != null ? MapTemplateToDto(template) : null;
        }

        // ========== توليد العقد ==========

        public async Task<ContractResponseDto?> GenerateContractFromTemplateAsync(Guid userId, GenerateContractRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var template = await _context.ContractTemplates.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive);
            if (template == null) return null;

            var contract = new Contract
            {
                Id = Guid.NewGuid(),
                ContractNumber = GenerateContractNumber(),
                UserId = userId,
                LawyerId = request.LawyerId,
                Title = request.ContractTitle ?? template.Name,
                Type = template.Type,
                Content = JsonSerializer.Serialize(request.FilledData),
                FileUrl = $"/uploads/contracts/user/{userId}/{Guid.NewGuid()}.docx",
                FileFormat = "docx",
                Status = ContractStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
            return MapToDto(contract);
        }

        // ========== عقود المستخدم ==========

        public async Task<List<ContractResponseDto>> GetUserContractsAsync(Guid userId, string? status = null, string? search = null)
        {
            var query = _context.Contracts.Include(c => c.User).Where(c => c.UserId == userId);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContractStatus>(status, true, out var s))
                query = query.Where(c => c.Status == s);
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(search) || c.ContractNumber.ToLower().Contains(search));
            }
            return (await query.OrderByDescending(c => c.CreatedAt).ToListAsync()).Select(MapToDto).ToList();
        }

        // ========== بحث وعرض عام (Public) ==========

        public async Task<List<ContractResponseDto>> SearchAllContractsAsync(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            var query = _context.Contracts.Include(c => c.User)
                .Where(c => c.Title.ToLower().Contains(searchTerm) || c.ContractNumber.ToLower().Contains(searchTerm));
            return (await query.OrderByDescending(c => c.CreatedAt).ToListAsync()).Select(MapToDto).ToList();
        }

        public async Task<ContractResponseDto?> GetAnyContractByIdAsync(Guid contractId)
        {
            var contract = await _context.Contracts.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == contractId);
            return contract != null ? MapToDto(contract) : null;
        }

        // ========== تعديل وحذف (مالك العقد فقط) ==========

        public async Task<ContractResponseDto?> UpdateContractAsync(Guid userId, Guid contractId, UpdateContractDto request)
        {
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);
            if (contract == null) return null;

            if (!string.IsNullOrEmpty(request.Title)) contract.Title = request.Title;
            if (request.ProgressPercentage.HasValue) contract.ProgressPercentage = request.ProgressPercentage.Value;
            contract.LastModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToDto(contract);
        }

        public async Task<bool> UpdateContractStatusAsync(Guid userId, Guid contractId, UpdateContractStatusDto request)
        {
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId);
            if (contract == null || contract.UserId != userId) return false;

            contract.Status = request.Status;
            if (request.Status == ContractStatus.Active) contract.SignedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteContractAsync(Guid userId, Guid contractId)
        {
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);
            if (contract == null) return false;

            if (!string.IsNullOrEmpty(contract.FileUrl))
            {
                var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", contract.FileUrl.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
            return true;
        }

        // ========== تحميل ==========

        public async Task<byte[]?> DownloadAnyContractAsync(Guid contractId)
        {
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId);
            if (contract == null || string.IsNullOrEmpty(contract.FileUrl)) return null;

            var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", contract.FileUrl.TrimStart('/'));
            return File.Exists(filePath) ? await File.ReadAllBytesAsync(filePath) : null;
        }

        // ========== Helpers ==========

        private string GenerateContractNumber()
        {
            var count = _context.Contracts.Count() + 1;
            return $"CNT-{DateTime.UtcNow.Year}-{count:D6}";
        }
 
        private ContractResponseDto MapToDto(Contract c) => new()
        {
            Id = c.Id, ContractNumber = c.ContractNumber, Title = c.Title, Type = c.Type,
            Content = c.Content, FileUrl = c.FileUrl, Status = c.Status,
            ProgressPercentage = c.ProgressPercentage, CreatedAt = c.CreatedAt,
            User = new UserBriefDto { Id = c.User?.UserID ?? Guid.Empty, FullName = c.User?.FullName ?? "", Email = c.User?.Email ?? "" }
        };

        private ContractTemplateResponseDto MapTemplateToDto(ContractTemplate t) => new()
        {
            Id = t.Id, Name = t.Name, Type = t.Type, Description = t.Description,
            TemplateContent = t.TemplateContent, IsActive = t.IsActive, CreatedAt = t.CreatedAt
        };
    }
}