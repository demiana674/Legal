// LegalMateAI.Domain/Entities/Law.cs
using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    /// <summary>
    /// القوانين في النظام - يضيفها الأدمن أو يرفعها المستخدم
    /// </summary>
    public class Law
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// اسم القانون
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// رقم القانون (اختياري)
        /// </summary>
        public string? LawNumber { get; set; }
        
        /// <summary>
        /// سنة الإصدار (اختياري)
        /// </summary>
        public int? Year { get; set; }
        
        /// <summary>
        /// تصنيف القانون
        /// </summary>
        public LawCategory Category { get; set; }
        
        /// <summary>
        /// وصف مختصر
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// مسار ملف PDF
        /// </summary>
        public string PdfFilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// رابط المصدر الأصلي
        /// </summary>
        public string? SourceUrl { get; set; }
        
        /// <summary>
        /// كلمات مفتاحية للبحث
        /// </summary>
        public string? SearchKeywords { get; set; }
        
        /// <summary>
        /// عدد مرات التحميل
        /// </summary>
        public int DownloadCount { get; set; }
        
        /// <summary>
        /// عدد مرات المشاهدة
        /// </summary>
        public int ViewCount { get; set; }
        
        /// <summary>
        /// هل القانون نشط؟
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// هل تمت الموافقة عليه من الأدمن؟
        /// </summary>
        public bool IsApproved { get; set; } = false;
        
        /// <summary>
        /// تاريخ الإضافة
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// معرف الأدمن الذي أضاف القانون (لو أضافه الأدمن)
        /// </summary>
        public Guid? AddedByAdminId { get; set; }
        public Admin? AddedByAdmin { get; set; }
        
        /// <summary>
        /// معرف المستخدم الذي رفع القانون (لو رفعه مستخدم عادي)
        /// </summary>
        public Guid? UploadedByUserId { get; set; }
        public User? UploadedByUser { get; set; }
        
        /// <summary>
        /// معرف الأدمن الذي وافق على القانون
        /// </summary>
        public Guid? ApprovedByAdminId { get; set; }
        public Admin? ApprovedByAdmin { get; set; }
        
        /// <summary>
        /// تاريخ الموافقة
        /// </summary>
        public DateTime? ApprovedAt { get; set; }
        
        /// <summary>
        /// سبب الرفض (لو تم رفضه)
        /// </summary>
        public string? RejectionReason { get; set; }
    }
}