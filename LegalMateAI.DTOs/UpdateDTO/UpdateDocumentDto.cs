using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class UpdateDocumentDto
    {
        // [StringLength(500)]
        public string? Description { get; set; }

        public DocumentType? DocType { get; set; }
    }
}