// LegalMateAI.BLL/Services/IService/ILegalService.cs
using LegalMateAI.DTOs.ReadDTO;  // ✅ استخدم DTOs من هنا
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ILegalService
    {
        Task<LawSearchResponseDto> SmartSearchAsync(string query, int page = 1, int pageSize = 10);
        Task<LawSearchResponseDto> SearchLawsAsync(string query, int page = 1, int pageSize = 10);
        Task<EgyptianLawResponseDto?> GetLawByIdAsync(int lawId);
        Task<LawArticleDetailedDto?> GetArticleByIdAsync(int articleId);
        Task<List<EgyptianLawResponseDto>> GetAllLawsAsync(LawCategory? category = null);
        Task<List<LawAmendmentBriefDto>> GetLawAmendmentsAsync(int lawId);
        
        // ✅ استخدم DTOs.ReadDTO.LawInterpretationDto
        Task<List<LawInterpretationDto>> GetArticleInterpretationsAsync(int articleId);
        
        Task SaveSearchQueryAsync(Guid userId, string query, int resultCount);
        Task<List<SearchQueryDto>> GetUserSearchHistoryAsync(Guid userId);
    }
}