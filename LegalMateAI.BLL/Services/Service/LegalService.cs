using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.BLL.Services.IService;

namespace LegalMateAI.BLL.Services.Service
{
    public class LegalService : ILegalService
    {
        private readonly LegalMateDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<LegalService> _logger;

        public LegalService(
            LegalMateDbContext context,
            IAIService aiService,
            ILogger<LegalService> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// بحث ذكي في القوانين باستخدام الذكاء الاصطناعي
        /// </summary>
        public async Task<LawSearchResponseDto> SmartSearchAsync(string query, int page = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Smart search: Query={Query}", 
                    query.Length > 50 ? query.Substring(0, 50) + "..." : query);
                
                // Call AI Service for semantic search
                var aiResults = await _aiService.SmartSearchAsync(query, pageSize * 2);
                
                // Traditional database search
                var searchTerm = query.ToLower();
                var dbResults = await _context.EgyptianLaws
                    .Where(l => l.TitleAr.Contains(searchTerm) || 
                                l.LawNumber.Contains(searchTerm))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                var totalCount = await _context.EgyptianLaws
                    .CountAsync(l => l.TitleAr.Contains(searchTerm) || 
                                     l.LawNumber.Contains(searchTerm));
                
                // Combine results
                var results = new List<LawSearchResultDto>();
                
                // Add AI results
                foreach (var aiResult in aiResults)
                {
                    results.Add(new LawSearchResultDto
                    {
                        Id = aiResult.Id,
                        Type = "Document",
                        Title = aiResult.Title ?? "نتيجة بحث",
                        Context = aiResult.Snippet?.Length > 200 
                            ? aiResult.Snippet.Substring(0, 200) + "..." 
                            : aiResult.Snippet ?? "",
                        Relevance = aiResult.Relevance,
                        Url = $"/legal/search/result/{aiResult.Id}"
                    });
                }
                
                // Add database results
                foreach (var law in dbResults)
                {
                    results.Add(new LawSearchResultDto
                    {
                        Id = law.Id.ToString(),
                        Type = "Law",
                        Title = law.TitleAr,
                        Context = law.Description?.Length > 200 
                            ? law.Description.Substring(0, 200) + "..." 
                            : law.Description ?? "",
                        Relevance = CalculateRelevance(query, law.TitleAr),
                        Url = $"/legal/laws/{law.Id}"
                    });
                }
                
                // Sort by relevance and limit
                results = results.OrderByDescending(r => r.Relevance).Take(pageSize).ToList();
                
                return new LawSearchResponseDto
                {
                    Query = query,
                    TotalResults = totalCount + aiResults.Count,
                    Results = results
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in smart search");
                return new LawSearchResponseDto
                {
                    Query = query,
                    TotalResults = 0,
                    Results = new List<LawSearchResultDto>()
                };
            }
        }

        /// <summary>
        /// البحث التقليدي في القوانين
        /// </summary>
        public async Task<LawSearchResponseDto> SearchLawsAsync(string query, int page = 1, int pageSize = 10)
        {
            var searchTerm = query.ToLower();
            
            var laws = await _context.EgyptianLaws
                .Where(l => l.TitleAr.Contains(searchTerm) || 
                            l.LawNumber.Contains(searchTerm))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            var totalCount = await _context.EgyptianLaws
                .CountAsync(l => l.TitleAr.Contains(searchTerm) || 
                                 l.LawNumber.Contains(searchTerm));
            
            var results = laws.Select(l => new LawSearchResultDto
            {
                Id = l.Id.ToString(),
                Type = "Law",
                Title = l.TitleAr,
                Context = l.Description?.Length > 200 ? l.Description.Substring(0, 200) + "..." : l.Description ?? "",
                Relevance = CalculateRelevance(query, l.TitleAr),
                Url = $"/legal/laws/{l.Id}"
            }).ToList();
            
            return new LawSearchResponseDto
            {
                Query = query,
                TotalResults = totalCount,
                Results = results
            };
        }

        /// <summary>
        /// الحصول على قانون محدد
        /// </summary>
        public async Task<EgyptianLawResponseDto?> GetLawByIdAsync(int lawId)
        {
            var law = await _context.EgyptianLaws
                .Include(l => l.Articles)
                .Include(l => l.Amendments)
                .Include(l => l.Keywords)
                .FirstOrDefaultAsync(l => l.Id == lawId);
            
            if (law == null) return null;
            
            law.ViewCount++;
            await _context.SaveChangesAsync();
            
            return new EgyptianLawResponseDto
            {
                Id = law.Id,
                LawNumber = law.LawNumber,
                TitleAr = law.TitleAr,
                Year = law.Year,
                Category = law.Category,
                Status = law.Status,
                Description = law.Description,
                PublishedAt = law.PublishedAt,
                LastAmendedAt = law.LastAmendedAt,
                ViewCount = law.ViewCount,
                ArticlesCount = law.Articles?.Count ?? 0,
                Articles = law.Articles?.Select(a => new LawArticleBriefDto
                {
                    Id = a.Id,
                    ArticleNumber = a.ArticleNumber,
                    Title = a.Title,
                    Content = a.Content.Length > 300 ? a.Content.Substring(0, 300) + "..." : a.Content,
                    IsActive = a.IsActive
                }).ToList() ?? new List<LawArticleBriefDto>(),
                Amendments = law.Amendments?.Select(a => new LawAmendmentBriefDto
                {
                    Id = a.Id,
                    AmendmentNumber = a.AmendmentNumber,
                    Title = a.Title,
                    AmendmentDate = a.AmendmentDate,
                    EffectiveDate = a.EffectiveDate,
                    Description = a.Description
                }).ToList() ?? new List<LawAmendmentBriefDto>(),
                Keywords = law.Keywords?.Select(k => k.Keyword).ToArray()
            };
        }

        /// <summary>
        /// الحصول على مادة قانونية محددة
        /// </summary>
        public async Task<LawArticleDetailedDto?> GetArticleByIdAsync(int articleId)
        {
            var article = await _context.LawArticles
                .Include(a => a.Law)
                .Include(a => a.Clauses)
                .FirstOrDefaultAsync(a => a.Id == articleId);
            
            if (article == null) return null;
            
            return new LawArticleDetailedDto
            {
                Id = article.Id,
                ArticleNumber = article.ArticleNumber,
                Title = article.Title,
                Content = article.Content,
                Summary = article.Summary,
                IsActive = article.IsActive,
                LawId = article.LawId,
                LawName = article.Law?.TitleAr ?? "",
                AmendedAt = article.AmendedAt,
                AmendmentDescription = article.AmendmentDescription,
                Clauses = article.Clauses?.Select(c => new ArticleClauseDto
                {
                    Id = c.Id,
                    ClauseNumber = c.ClauseNumber,
                    Content = c.Content,
                    Order = c.Order
                }).ToList() ?? new List<ArticleClauseDto>(),
                Interpretations = new List<LawInterpretationDto>()
            };
        }

        /// <summary>
        /// الحصول على جميع القوانين
        /// </summary>
        public async Task<List<EgyptianLawResponseDto>> GetAllLawsAsync(LawCategory? category = null)
        {
            var query = _context.EgyptianLaws.AsQueryable();
            
            if (category.HasValue)
                query = query.Where(l => l.Category == category.Value);
            
            var laws = await query
                .OrderBy(l => l.Category)
                .ThenBy(l => l.Year)
                .ToListAsync();
            
            return laws.Select(l => new EgyptianLawResponseDto
            {
                Id = l.Id,
                LawNumber = l.LawNumber,
                TitleAr = l.TitleAr,
                Year = l.Year,
                Category = l.Category,
                Status = l.Status,
                Description = l.Description,
                PublishedAt = l.PublishedAt,
                LastAmendedAt = l.LastAmendedAt,
                ViewCount = l.ViewCount,
                ArticlesCount = l.Articles?.Count ?? 0
            }).ToList();
        }

        /// <summary>
        /// الحصول على تعديلات قانون
        /// </summary>
        public async Task<List<LawAmendmentBriefDto>> GetLawAmendmentsAsync(int lawId)
        {
            var amendments = await _context.LawAmendments
                .Where(a => a.LawId == lawId)
                .OrderByDescending(a => a.AmendmentDate)
                .ToListAsync();
            
            return amendments.Select(a => new LawAmendmentBriefDto
            {
                Id = a.Id,
                AmendmentNumber = a.AmendmentNumber,
                Title = a.Title,
                AmendmentDate = a.AmendmentDate,
                EffectiveDate = a.EffectiveDate,
                Description = a.Description
            }).ToList();
        }

        /// <summary>
        /// الحصول على تفسيرات مادة قانونية
        /// </summary>
        public async Task<List<LawInterpretationDto>> GetArticleInterpretationsAsync(int articleId)
        {
            var interpretations = await _context.LawInterpretations
                .Where(i => i.ArticleId == articleId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
            
            return interpretations.Select(i => new LawInterpretationDto
            {
                Id = i.Id,
                Title = i.Title,
                Content = i.Content,
                Source = i.Source,
                SourceReference = i.SourceReference,
                CreatedAt = i.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// حفظ بحث المستخدم
        /// </summary>
        public async Task SaveSearchQueryAsync(Guid userId, string query, int resultCount)
        {
            var searchQuery = new SearchQuery
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Query = query,
                ResultCount = resultCount,
                SearchedAt = DateTime.UtcNow
            };
            
            _context.SearchQueries.Add(searchQuery);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// الحصول على سجل بحث المستخدم
        /// </summary>
        public async Task<List<SearchQueryDto>> GetUserSearchHistoryAsync(Guid userId)
        {
            var history = await _context.SearchQueries
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.SearchedAt)
                .Take(50)
                .ToListAsync();
            
            return history.Select(h => new SearchQueryDto
            {
                Id = h.Id,
                Query = h.Query,
                ProcessedIntent = h.ProcessedIntent,
                ResultCount = h.ResultCount,
                SearchedAt = h.SearchedAt
            }).ToList();
        }

        #region Private Methods

        private double CalculateRelevance(string query, string title)
        {
            var queryWords = query.ToLower().Split(' ');
            var titleLower = title.ToLower();
            
            var matches = queryWords.Count(w => titleLower.Contains(w));
            return (double)matches / queryWords.Length;
        }

        #endregion
    }
}