// LegalMateAI.Domain/Entities/CaseDocument.cs
using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class CaseDocument
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public Case Case { get; set; } = null!;
        
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? FileType { get; set; }
        public long FileSize { get; set; }
        public string? Description { get; set; }
        
        public Guid UploadedBy { get; set; }  // UserId of uploader
        public DateTime UploadedAt { get; set; }
        
        public bool IsVerified { get; set; }
    }
}