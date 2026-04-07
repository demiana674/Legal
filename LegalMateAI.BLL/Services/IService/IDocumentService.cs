using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IDocumentService
    {
        Task<DocumentResponseDto?> UploadDocumentAsync(Guid userId, CreateDocumentDto request);
        Task<List<DocumentResponseDto>> GetUserDocumentsAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<DocumentResponseDto?> GetDocumentByIdAsync(Guid userId, Guid documentId);
        Task<bool> DeleteDocumentAsync(Guid userId, Guid documentId);
        Task<byte[]?> DownloadDocumentAsync(Guid userId, Guid documentId);
    }
}