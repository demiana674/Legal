using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class CaseResponseDto
    {
        public Guid Id { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Client Info
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        
        // ✅ إضافة البريد الإلكتروني ورقم الهاتف للموكل
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        
        // Lawyer Info
        public Guid? LawyerId { get; set; }
        public string? LawyerName { get; set; }
        
        // Case Details
        public string? Court { get; set; }
        public DateTime? NextHearingDate { get; set; }
        public CaseStatus Status { get; set; }
        public CasePriority Priority { get; set; }
        public string? CaseType { get; set; }
        
        // Dates
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        
        // Counts
        public int DocumentsCount { get; set; }
        public int NotesCount { get; set; }
        
        // Navigation Properties
        public List<CaseDocumentResponseDto> Documents { get; set; } = new();
        public List<CaseNoteResponseDto> Notes { get; set; } = new();
    }
}