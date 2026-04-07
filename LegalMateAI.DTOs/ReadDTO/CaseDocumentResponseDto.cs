// LegalMateAI.DTOs/ReadDTO/CaseDocumentResponseDto.cs
using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class CaseDocumentResponseDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? FileType { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string UploadedAtFormatted => UploadedAt.ToString("dd MMM yyyy HH:mm");
        public bool IsVerified { get; set; }
    }
}