// LegalMateAI.BLL/Services/IService/ICaseService.cs
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ICaseService
    {
        // ========== CRUD للقضايا ==========
        Task<CaseResponseDto?> CreateCaseAsync(Guid userId, CreateCaseDto request, bool isLawyer = false);
        Task<CaseResponseDto?> UpdateCaseAsync(Guid userId, Guid caseId, UpdateCaseDto request, bool isLawyer = false);
        Task<bool> DeleteCaseAsync(Guid userId, Guid caseId, bool isLawyer = false);
        
        // ========== جلب القضايا ==========
        Task<List<CaseResponseDto>> GetCasesAsync(CaseFilterDto filter);
        Task<CaseResponseDto?> GetCaseByIdAsync(Guid userId, Guid caseId, bool isLawyer = false);
        
        // ========== إدارة المستندات ==========
        Task<CaseDocumentResponseDto?> UploadDocumentAsync(Guid userId, CreateCaseDocumentDto request);
        Task<bool> DeleteDocumentAsync(Guid userId, Guid documentId);
        Task<byte[]?> DownloadDocumentAsync(Guid documentId);
        Task<CaseDocumentResponseDto?> GetDocumentByIdAsync(Guid documentId);
        
        // ========== إدارة الملاحظات ==========
        Task<CaseNoteResponseDto?> AddNoteAsync(Guid userId, CreateCaseNoteDto request, bool isLawyer = false);
        Task<CaseNoteResponseDto?> UpdateNoteAsync(Guid userId, Guid noteId, string content);
        Task<bool> DeleteNoteAsync(Guid userId, Guid noteId);
        
        // ========== إحصائيات ==========
        Task<CaseStatsDto> GetCaseStatsAsync(Guid? lawyerId = null, Guid? clientId = null);
    }
    
    public class CaseStatsDto
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Pending { get; set; }
        public int Completed { get; set; }
        public int Rejected { get; set; }
        public int OnHold { get; set; }
        public int Urgent { get; set; }
        public int UpcomingHearings { get; set; }
    }
}