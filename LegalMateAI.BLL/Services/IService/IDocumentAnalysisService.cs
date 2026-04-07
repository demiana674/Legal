// LegalMateAI.BLL/Services/IService/IDocumentAnalysisService.cs
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IDocumentAnalysisService
    {
        Task<DocumentAnalysisResponseDto?> AnalyzeDocumentAsync(Guid userId, Guid documentId, CreateDocumentAnalysisDto request);
        Task<DocumentAnalysisResponseDto?> GetAnalysisByDocumentAsync(Guid userId, Guid documentId);
        Task<List<DocumentAnalysisResponseDto>> GetUserAnalysesAsync(Guid userId);
    }
}