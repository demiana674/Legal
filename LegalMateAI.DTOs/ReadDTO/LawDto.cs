using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LawNumber { get; set; }
        public int? Year { get; set; }
        public LawCategory Category { get; set; }
        
        // تم التغيير: خاصية قابلة للقراءة والكتابة مع تعيين افتراضي فارغ
        public string CategoryName { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        public string? PdfFileUrl { get; set; }
        public string? SourceUrl { get; set; }
        public List<string> SearchKeywords { get; set; } = new();
        public int DownloadCount { get; set; }
        public int ViewCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => CreatedAt.ToString("dd MMM yyyy");
        public string AddedByAdminName { get; set; } = string.Empty;
        public string? UploadedByUserName { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        public bool HasPdfLink => !string.IsNullOrEmpty(PdfFileUrl);
        public bool HasSourceLink => !string.IsNullOrEmpty(SourceUrl);
    }
}