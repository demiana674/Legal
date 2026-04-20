// LegalMateAI.Domain/Entities/PredefinedContractTemplate.cs
using System;
using System.ComponentModel.DataAnnotations.Schema;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    /// <summary>
    /// قوالب عقود جاهزة - تدعم PDF و Word
    /// </summary>
    public class PredefinedContractTemplate
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// اسم القالب (مثال: "عقد إيجار سكني")
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// اسم القالب بالإنجليزية (للبحث)
        /// </summary>
        public string? NameEn { get; set; }
        
        /// <summary>
        /// الوصف
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// نوع العقد
        /// </summary>
        public ContractType ContractType { get; set; }
        
        /// <summary>
        /// كلمات مفتاحية للبحث (مفصولة بفواصل)
        /// مثال: "إيجار,شقة,سكني,تمليك"
        /// </summary>
        public string? SearchKeywords { get; set; }
        
        /// <summary>
        /// صيغة الملف: "pdf" أو "docx"
        /// </summary>
        public string FileFormat { get; set; } = "pdf";
        
        /// <summary>
        /// مسار الملف على السيرفر
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// مسار صورة مصغرة (اختياري)
        /// </summary>
        public string? ThumbnailPath { get; set; }
        
        /// <summary>
        /// الحقول المطلوبة (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string RequiredFieldsJson { get; set; } = "[]";
        
        /// <summary>
        /// عدد مرات التحميل
        /// </summary>
        public int DownloadCount { get; set; }
        
        /// <summary>
        /// عدد مرات الاستخدام
        /// </summary>
        public int UsageCount { get; set; }
        
        /// <summary>
        /// التقييم (1-5)
        /// </summary>
        public double? Rating { get; set; }
        
        /// <summary>
        /// هل القالب نشط؟
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// هل القالب مميز (يظهر في الصفحة الرئيسية)؟
        /// </summary>
        public bool IsFeatured { get; set; }
        
        /// <summary>
        /// تاريخ الإنشاء
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// تاريخ آخر تحديث
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// معرف الأدمن الذي أنشأ القالب
        /// </summary>
        public Guid CreatedByAdminId { get; set; }
        
        /// <summary>
        /// Navigation Properties
        /// </summary>
        public Admin CreatedByAdmin { get; set; } = null!;
        public ICollection<GeneratedContract> GeneratedContracts { get; set; } = new List<GeneratedContract>();
    }
}