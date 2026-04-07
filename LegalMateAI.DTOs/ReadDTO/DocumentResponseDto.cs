using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class DocumentResponseDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileSizeFormatted { get; set; } = string.Empty;
        public DocumentType DocType { get; set; }
        public string DocTypeName => DocType.ToString();
        public string? Description { get; set; }
        public DocumentStatus Status { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedAtFormatted => UploadedAt.ToString("dd MMM yyyy");
        public DateTime? ExpiryDate { get; set; }
        public bool HasAnalysis { get; set; }
        
        // ✅ إضافة UserId للتحقق من الصلاحية
        public Guid UserId { get; set; }
    }
}