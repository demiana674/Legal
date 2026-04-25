// LegalMateAI.Domain/Entities/Law.cs
using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class Law
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LawNumber { get; set; }
        public int? Year { get; set; }
        public LawCategory Category { get; set; }
        public string? Description { get; set; }
        
        /// <summary>
        /// رابط تحميل PDF مباشر (من الموقع الأصلي - مش متخزن عندنا)
        /// </summary>
        public string? PdfFileUrl { get; set; }
        
        /// <summary>
        /// رابط المصدر الرسمي للقانون
        /// </summary>
        public string? SourceUrl { get; set; }
        
        public string? SearchKeywords { get; set; }
        public int DownloadCount { get; set; }
        public int ViewCount { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public Guid? AddedByAdminId { get; set; }
        public Admin? AddedByAdmin { get; set; }
        public Guid? UploadedByUserId { get; set; }
        public User? UploadedByUser { get; set; }
        public Guid? ApprovedByAdminId { get; set; }
        public Admin? ApprovedByAdmin { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        
        // ✅ إزالة PdfFilePath (مش هنستخدمه)
    }
}