// LegalMateAI.Domain/Entities/LawyerSkill.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMateAI.Domain.Entities
{
    /// <summary>
    /// قائمة المهارات والكفاءات المتاحة للمحامين
    /// </summary>
    public class LawyerSkill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// اسم المهارة بالإنجليزية
        /// </summary>
        [Required(ErrorMessage = "اسم المهارة بالإنجليزية مطلوب")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// اسم المهارة بالعربية
        /// </summary>
        [Required(ErrorMessage = "اسم المهارة بالعربية مطلوب")]
        [StringLength(100)]
        public string NameAr { get; set; } = string.Empty;

        /// <summary>
        /// وصف المهارة (اختياري)
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// هل المهارة نشطة؟
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// ترتيب العرض
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// تاريخ الإنشاء
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// أيقونة المهارة (اختياري)
        /// </summary>
        [StringLength(50)]
        public string? Icon { get; set; }

        /// <summary>
        /// التصنيف الفرعي للمهارة
        /// </summary>
        [StringLength(50)]
        public string? Category { get; set; }
    }
}