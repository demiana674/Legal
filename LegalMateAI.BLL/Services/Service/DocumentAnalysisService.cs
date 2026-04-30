using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.BLL.Services.IService;
using Microsoft.Extensions.Logging; 
namespace LegalMateAI.BLL.Services.Service
{
    public class DocumentAnalysisService : IDocumentAnalysisService
    {
        private readonly LegalMateDbContext _context;
        private readonly IAIService _aiService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DocumentAnalysisService> _logger;

        public DocumentAnalysisService(
            LegalMateDbContext context,
            IAIService aiService,
            IWebHostEnvironment env,
            ILogger<DocumentAnalysisService> logger)
        {
            _context = context;
            _aiService = aiService;
            _env = env;
            _logger = logger;
        }

        public async Task<DocumentAnalysisResponseDto?> AnalyzeDocumentAsync(
            Guid userId, Guid documentId, CreateDocumentAnalysisDto request)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);
            
            if (document == null)
            {
                _logger.LogWarning("Document not found: {DocumentId}", documentId);
                return null;
            }

            var existingAnalysis = await _context.DocumentAnalyses
                .FirstOrDefaultAsync(a => a.DocumentId == documentId);
            
            if (existingAnalysis != null && existingAnalysis.Status == AnalysisStatus.Completed)
            {
                return await GetAnalysisByDocumentAsync(userId, documentId);
            }

            try
            {
                if (existingAnalysis != null)
                {
                    existingAnalysis.Status = AnalysisStatus.Processing;
                    existingAnalysis.RequestedAt = DateTime.UtcNow;
                }
                else
                {
                    var newAnalysis = new DocumentAnalysis
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = documentId,
                        UserId = userId,
                        Status = AnalysisStatus.Processing,
                        RequestedAt = DateTime.UtcNow
                    };
                    _context.DocumentAnalyses.Add(newAnalysis);
                }
                await _context.SaveChangesAsync();

                var filePath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), 
                    document.FileUrl.TrimStart('/'));
                
                if (!File.Exists(filePath))
                {
                    _logger.LogError("File not found: {FilePath}", filePath);
                    await UpdateAnalysisStatus(documentId, AnalysisStatus.Failed, "فشل في قراءة الملف");
                    return null;
                }

                var fileContent = await File.ReadAllBytesAsync(filePath);
                var aiResult = await _aiService.AnalyzeDocumentAsync(document, fileContent);
                
                var analysis = await _context.DocumentAnalyses
                    .FirstOrDefaultAsync(a => a.DocumentId == documentId);
                
                if (analysis != null)
                {
                    analysis.ExtractedText = aiResult.ExtractedText;
                    analysis.Summary = aiResult.Summary;
                    analysis.Result = aiResult.Result;
                    analysis.Status = AnalysisStatus.Completed;
                    analysis.CompletedAt = DateTime.UtcNow;
                    
                    foreach (var clause in aiResult.Clauses)
                    {
                        _context.ClauseAnalyses.Add(new ClauseAnalysis
                        {
                            Id = Guid.NewGuid(),
                            AnalysisId = analysis.Id,
                            ClauseTitle = clause.Title,
                            ClauseText = clause.Text,
                            PageNumber = clause.PageNumber,
                            Interpretation = clause.Interpretation
                        });
                    }
                    
                    foreach (var risk in aiResult.Risks)
                    {
                        _context.RiskAssessments.Add(new RiskAssessment
                        {
                            Id = Guid.NewGuid(),
                            AnalysisId = analysis.Id,
                            RiskType = risk.Type,
                            Description = risk.Description,
                            Level = RiskMapper.MapToRiskLevel(risk.Level),
                            Suggestion = risk.Suggestion
                        });
                    }
                    
                    await _context.SaveChangesAsync();
                }
                
                return await GetAnalysisByDocumentAsync(userId, documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document: {DocumentId}", documentId);
                await UpdateAnalysisStatus(documentId, AnalysisStatus.Failed, $"خطأ في التحليل: {ex.Message}");
                return null;
            }
        }

        private async Task UpdateAnalysisStatus(Guid documentId, AnalysisStatus status, string result)
        {
            var analysis = await _context.DocumentAnalyses
                .FirstOrDefaultAsync(a => a.DocumentId == documentId);
            
            if (analysis != null)
            {
                analysis.Status = status;
                analysis.Result = result;
                if (status == AnalysisStatus.Completed)
                    analysis.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<DocumentAnalysisResponseDto?> GetAnalysisByDocumentAsync(Guid userId, Guid documentId)
        {
            var analysis = await _context.DocumentAnalyses
                .Include(a => a.Clauses)
                .Include(a => a.Risks)
                .FirstOrDefaultAsync(a => a.DocumentId == documentId && a.UserId == userId);
            
            if (analysis == null) return null;
            
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId);
            
            return new DocumentAnalysisResponseDto
            {
                Id = analysis.Id,
                DocumentId = documentId,
                DocumentName = document?.FileName ?? "",
                Summary = analysis.Summary,
                Status = analysis.Status,
                RequestedAt = analysis.RequestedAt,
                CompletedAt = analysis.CompletedAt,
                Clauses = analysis.Clauses.Select(c => new ClauseAnalysisDto
                {
                    Id = c.Id,
                    ClauseTitle = c.ClauseTitle,
                    ClauseText = c.ClauseText,
                    PageNumber = c.PageNumber,
                    Interpretation = c.Interpretation
                }).ToList(),
                Risks = analysis.Risks.Select(r => new RiskAssessmentDto
                {
                    Id = r.Id,
                    RiskType = r.RiskType,
                    Description = r.Description,
                    Level = r.Level,
                    Suggestion = r.Suggestion
                }).ToList()
            };
        }

        public async Task<List<DocumentAnalysisResponseDto>> GetUserAnalysesAsync(Guid userId)
        {
            var analyses = await _context.DocumentAnalyses
                .Include(a => a.Clauses)
                .Include(a => a.Risks)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();
            
            var documentIds = analyses.Select(a => a.DocumentId).Distinct();
            var documents = await _context.Documents
                .Where(d => documentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.FileName);
            
            return analyses.Select(a => new DocumentAnalysisResponseDto
            {
                Id = a.Id,
                DocumentId = a.DocumentId,
                DocumentName = documents.GetValueOrDefault(a.DocumentId) ?? "",
                Summary = a.Summary,
                Status = a.Status,
                RequestedAt = a.RequestedAt,
                CompletedAt = a.CompletedAt,
                Clauses = a.Clauses.Select(c => new ClauseAnalysisDto
                {
                    Id = c.Id,
                    ClauseTitle = c.ClauseTitle,
                    ClauseText = c.ClauseText,
                    PageNumber = c.PageNumber,
                    Interpretation = c.Interpretation
                }).ToList(),
                Risks = a.Risks.Select(r => new RiskAssessmentDto
                {
                    Id = r.Id,
                    RiskType = r.RiskType,
                    Description = r.Description,
                    Level = r.Level,
                    Suggestion = r.Suggestion
                }).ToList()
            }).ToList();
        }
    }
}