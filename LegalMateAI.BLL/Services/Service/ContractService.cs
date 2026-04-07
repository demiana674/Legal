// LegalMateAI.BLL/Services/Service/ContractService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using System.Text;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class ContractService : IContractService
    {
        private readonly LegalMateDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<ContractService> _logger;
        private readonly PdfGenerationService _pdfService;

        public ContractService(
            LegalMateDbContext context, 
            IAIService aiService,
            ILogger<ContractService> logger,
            PdfGenerationService pdfService)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
            _pdfService = pdfService;
        }

        // ========== Contract CRUD ==========
        
        public async Task<ContractResponseDto?> CreateContractAsync(Guid userId, CreateContractDto request)
        {
            _logger.LogInformation($"CreateContractAsync called by user: {userId}");
            
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            if (request.LawyerId.HasValue)
            {
                var lawyer = await _context.LawyerProfiles
                    .FirstOrDefaultAsync(l => l.UserId == request.LawyerId);
                if (lawyer == null) return null;
            }

            var contractNumber = GenerateContractNumber();

            var contract = new Contract
            {
                Id = Guid.NewGuid(),
                ContractNumber = contractNumber,
                UserId = userId,
                LawyerId = request.LawyerId,
                Title = request.Title,
                Type = request.Type,
                PartyName = request.PartyName,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Value = request.Value,
                MonetaryValue = request.MonetaryValue,
                Status = ContractStatus.Draft,
                ProgressPercentage = 0,
                IsGeneratedByAI = request.TemplateId.HasValue,
                CreatedAt = DateTime.UtcNow
            };

            if (request.TemplateId.HasValue)
            {
                var template = await _context.ContractTemplates
                    .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive);

                if (template != null)
                {
                    var data = request.CustomFields ?? new Dictionary<string, string>();
                    data["PartyName"] = request.PartyName;
                    data["StartDate"] = request.StartDate.ToString("dd/MM/yyyy");
                    data["EndDate"] = request.EndDate?.ToString("dd/MM/yyyy") ?? "غير محدد";
                    data["Value"] = request.Value ?? "";

                    contract.Content = await _aiService.GenerateContractAsync(template, data);
                }
            }

            if (string.IsNullOrEmpty(contract.Content))
            {
                contract.Content = GenerateDefaultContractContent(contract);
            }

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            return await GetContractByIdAsync(userId, contract.Id, false);
        }

        public async Task<ContractResponseDto?> CreateContractFromTemplateAsync(Guid userId, Guid templateId, Dictionary<string, string> customFields)
        {
            _logger.LogInformation($"CreateContractFromTemplateAsync called by user: {userId}, TemplateId: {templateId}");
            
            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);

            if (template == null) return null;

            var contractNumber = GenerateContractNumber();

            var contract = new Contract
            {
                Id = Guid.NewGuid(),
                ContractNumber = contractNumber,
                UserId = userId,
                Title = template.Name,
                Type = template.Type,
                Status = ContractStatus.Draft,
                IsGeneratedByAI = true,
                CreatedAt = DateTime.UtcNow,
                Content = await _aiService.GenerateContractAsync(template, customFields)
            };

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            return await GetContractByIdAsync(userId, contract.Id, false);
        }

        public async Task<List<ContractResponseDto>> GetUserContractsAsync(Guid userId, string? status = null)
        {
            _logger.LogInformation($"GetUserContractsAsync called for user: {userId}");
            
            var query = _context.Contracts
                .Include(c => c.User)
                .Include(c => c.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(c => c.Clauses)
                .Where(c => c.UserId == userId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContractStatus>(status, true, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }

            var contracts = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return contracts.Select(c => MapToDto(c)).ToList();
        }

        public async Task<List<ContractResponseDto>> GetLawyerContractsAsync(Guid lawyerId, string? status = null)
        {
            _logger.LogInformation($"GetLawyerContractsAsync called for lawyer: {lawyerId}");
            
            var query = _context.Contracts
                .Include(c => c.User)
                .Include(c => c.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(c => c.Clauses)
                .Where(c => c.LawyerId == lawyerId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContractStatus>(status, true, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }

            var contracts = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return contracts.Select(c => MapToDto(c)).ToList();
        }

        public async Task<ContractResponseDto?> GetContractByIdAsync(Guid userId, Guid contractId, bool isLawyer = false)
        {
            _logger.LogInformation($"GetContractByIdAsync called: ContractId={contractId}, UserId={userId}, IsLawyer={isLawyer}");
            
            var contract = await _context.Contracts
                .Include(c => c.User)
                .Include(c => c.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(c => c.Clauses)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null) return null;

            if (!isLawyer && contract.UserId != userId)
                return null;
            if (isLawyer && contract.LawyerId != userId)
                return null;

            return MapToDto(contract);
        }

        public async Task<ContractResponseDto?> UpdateContractAsync(Guid userId, Guid contractId, UpdateContractDto request)
        {
            _logger.LogInformation($"UpdateContractAsync called: ContractId={contractId}, UserId={userId}");
            
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

            if (contract == null) return null;

            if (contract.Status == ContractStatus.Active || contract.Status == ContractStatus.Terminated)
                return null;

            bool hasChanges = false;

            if (!string.IsNullOrEmpty(request.Title))
            {
                contract.Title = request.Title;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.Content))
            {
                contract.Content = request.Content;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.PartyName))
            {
                contract.PartyName = request.PartyName;
                hasChanges = true;
            }

            if (request.StartDate.HasValue)
            {
                contract.StartDate = request.StartDate.Value;
                hasChanges = true;
            }

            if (request.EndDate.HasValue)
            {
                contract.EndDate = request.EndDate;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.Value))
            {
                contract.Value = request.Value;
                hasChanges = true;
            }

            if (request.MonetaryValue.HasValue)
            {
                contract.MonetaryValue = request.MonetaryValue;
                hasChanges = true;
            }

            if (request.ProgressPercentage.HasValue)
            {
                contract.ProgressPercentage = request.ProgressPercentage.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                contract.LastModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Contract {contractId} updated successfully");
            }

            return await GetContractByIdAsync(userId, contractId, false);
        }

        public async Task<bool> UpdateContractStatusAsync(Guid userId, Guid contractId, UpdateContractStatusDto request, bool isLawyer = false)
        {
            _logger.LogInformation($"UpdateContractStatusAsync called: ContractId={contractId}, UserId={userId}, IsLawyer={isLawyer}, NewStatus={request.Status}");
            
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null) return false;

            if (!isLawyer && contract.UserId != userId)
                return false;
            if (isLawyer && contract.LawyerId != userId)
                return false;

            contract.Status = request.Status;

            if (request.Status == ContractStatus.Active)
                contract.SignedAt = DateTime.UtcNow;

            if (request.Status == ContractStatus.Terminated)
                contract.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Contract {contractId} status updated to {request.Status}");
            return true;
        }

        public async Task<bool> DeleteContractAsync(Guid userId, Guid contractId)
        {
            _logger.LogInformation($"DeleteContractAsync called: ContractId={contractId}, UserId={userId}");
            
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

            if (contract == null) return false;

            if (contract.Status == ContractStatus.Active || contract.Status == ContractStatus.PendingSignature)
                return false;

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Contract {contractId} deleted successfully");
            return true;
        }

        public async Task<byte[]?> DownloadContractAsync(Guid userId, Guid contractId, string format = "pdf")
        {
            _logger.LogInformation($"DownloadContractAsync called: ContractId={contractId}, UserId={userId}, Format={format}");
            
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

            if (contract == null)
            {
                _logger.LogWarning($"Contract not found: {contractId}");
                return null;
            }

            try
            {
                if (format.ToLower() == "pdf")
                {
                    var pdfBytes = _pdfService.GenerateContractPdf(
                        contract.Title,
                        contract.ContractNumber,
                        contract.Content,
                        contract.CreatedAt
                    );
                    _logger.LogInformation($"PDF generated successfully, Size: {pdfBytes.Length} bytes");
                    return pdfBytes;
                }
                else if (format.ToLower() == "doc" || format.ToLower() == "docx")
                {
                    var wordBytes = _pdfService.GenerateContractWord(
                        contract.Title,
                        contract.ContractNumber,
                        contract.Content,
                        contract.CreatedAt
                    );
                    _logger.LogInformation($"Word document generated successfully");
                    return wordBytes;
                }
                
                return Encoding.UTF8.GetBytes(contract.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating document for contract {contractId}");
                return Encoding.UTF8.GetBytes(contract.Content);
            }
        }

        // ========== Contract Templates ==========
        
        public async Task<List<ContractTemplateResponseDto>> GetContractTemplatesAsync(ContractType? type = null)
        {
            _logger.LogInformation($"GetContractTemplatesAsync called with type: {type}");
            
            var query = _context.ContractTemplates
                .Where(t => t.IsActive);

            if (type.HasValue)
                query = query.Where(t => t.Type == type.Value);

            var templates = await query
                .OrderBy(t => t.Type)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return templates.Select(t => new ContractTemplateResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Type = t.Type,
                Description = t.Description,
                TemplateContent = t.TemplateContent,
                Placeholders = t.Placeholders,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList();
        }

        public async Task<ContractTemplateResponseDto?> CreateContractTemplateAsync(Guid adminId, CreateContractTemplateDto request)
        {
            _logger.LogInformation($"CreateContractTemplateAsync called by admin: {adminId}");
            
            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null)
            {
                _logger.LogWarning($"Admin not found: {adminId}");
                return null;
            }

            var template = new ContractTemplate
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Type = request.Type,
                Description = request.Description,
                TemplateContent = request.TemplateContent,
                Placeholders = request.Placeholders,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContractTemplates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Contract template created: {template.Name}");

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

        public async Task<ContractTemplateResponseDto?> UpdateContractTemplateAsync(Guid adminId, Guid templateId, UpdateContractTemplateDto request)
        {
            _logger.LogInformation($"UpdateContractTemplateAsync called by admin: {adminId}, TemplateId: {templateId}");
            
            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null)
            {
                _logger.LogWarning($"Admin not found: {adminId}");
                return null;
            }

            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null)
            {
                _logger.LogWarning($"Template not found: {templateId}");
                return null;
            }

            bool hasChanges = false;

            if (!string.IsNullOrEmpty(request.Name) && template.Name != request.Name)
            {
                template.Name = request.Name;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.Description) && template.Description != request.Description)
            {
                template.Description = request.Description;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.TemplateContent) && template.TemplateContent != request.TemplateContent)
            {
                template.TemplateContent = request.TemplateContent;
                hasChanges = true;
            }

            if (request.Placeholders != null)
            {
                template.Placeholders = request.Placeholders;
                hasChanges = true;
            }

            if (request.IsActive.HasValue && template.IsActive != request.IsActive.Value)
            {
                template.IsActive = request.IsActive.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                template.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Contract template updated: {template.Name}");
            }

            return new ContractTemplateResponseDto
            {
                Id = template.Id,
                Name = template.Name,
                Type = template.Type,
                Description = template.Description,
                TemplateContent = template.TemplateContent,
                Placeholders = template.Placeholders,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };
        }

        public async Task<bool> DeleteContractTemplateAsync(Guid adminId, Guid templateId)
        {
            _logger.LogInformation($"DeleteContractTemplateAsync called by admin: {adminId}, TemplateId: {templateId}");
            
            var admin = await _context.Admins.FindAsync(adminId);
            if (admin == null)
            {
                _logger.LogWarning($"Admin not found: {adminId}");
                return false;
            }

            var template = await _context.ContractTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null)
            {
                _logger.LogWarning($"Template not found: {templateId}");
                return false;
            }

            _context.ContractTemplates.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Contract template deleted: {template.Name}");
            return true;
        }

        // ========== Helper Methods ==========
        
        private string GenerateContractNumber()
        {
            var year = DateTime.UtcNow.Year;
            var count = _context.Contracts.Count() + 1;
            return $"CNT-{year}-{count:D6}";
        }

        private string GenerateDefaultContractContent(Contract contract)
        {
            var user = _context.Users.Find(contract.UserId);
            return $@"
                <h1>{contract.Title}</h1>
                <p><strong>رقم العقد:</strong> {contract.ContractNumber}</p>
                <p> {user?.FullName ?? "غير محدد"}<strong>:الطرف الأول</strong></p>
                <p><strong>الطرف الثاني:</strong> {contract.PartyName}</p>
                <p><strong>تاريخ البدء:</strong> {contract.StartDate:dd/MM/yyyy}</p>
                <p><strong>تاريخ الانتهاء:</strong> {(contract.EndDate?.ToString("dd/MM/yyyy") ?? "غير محدد")}</p>
                <p><strong>القيمة:</strong> {contract.Value ?? contract.MonetaryValue?.ToString("C") ?? "غير محدد"}</p>
                <hr/>
                <p>هذا العقد تم إنشاؤه بواسطة نظام LegalMate AI. يرجى مراجعة المحتوى والتوقيع عليه.</p>
            ";
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
        PartyName = contract.PartyName,
        StartDate = contract.StartDate,
        EndDate = contract.EndDate,
        Value = contract.Value,
        Status = contract.Status,
        ProgressPercentage = contract.ProgressPercentage,
        CreatedAt = contract.CreatedAt,
        LastModifiedAt = contract.LastModifiedAt,
        SignedAt = contract.SignedAt,
        IsGeneratedByAI = contract.IsGeneratedByAI,
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
            // ✅ التعديل هنا
            Specialization = lawyerProfile?.Specialties?.FirstOrDefault()?.Specialty?.NameAr ?? "",
            Rating = 0
        } : null,
        Clauses = contract.Clauses?.Select(c => new ContractClauseDto
        {
            Id = c.Id,
            ClauseTitle = c.ClauseTitle,
            ClauseContent = c.ClauseContent,
            Order = c.Order
        }).ToList() ?? new()
    };
}
    }
}