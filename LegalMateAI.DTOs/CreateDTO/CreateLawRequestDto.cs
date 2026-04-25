// LegalMateAI.DTOs/CreateDTO/CreateLawRequestDto.cs
using System.ComponentModel.DataAnnotations;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.CreateDTO
{
    /// <summary>
    /// نموذج إضافة قانون جديد (من المستخدم أو الأدمن)
    /// </summary>
    public class CreateLawRequestDto
    {
        /// <summary>
        /// رابط المصدر (الموقع اللي فيه القانون)
        /// </summary>
        [Required(ErrorMessage = "رابط المصدر مطلوب")]
        [Url(ErrorMessage = "رابط غير صحيح")]
        public string SourceUrl { get; set; } = string.Empty;

        /// <summary>
        /// اسم القانون (اختياري - لو فاضي هيتم استخراجه من الرابط)
        /// </summary>
        [StringLength(200)]
        public string? Name { get; set; }

        /// <summary>
        /// رقم القانون (اختياري - لو فاضي هيتم استخراجه من الرابط)
        /// </summary>
        [StringLength(50)]
        public string? LawNumber { get; set; }

        /// <summary>
        /// سنة الإصدار (اختياري - لو فاضي هيتم استخراجه من الرابط)
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// التصنيف (اختياري - لو فاضي هيتم استخراجه من الرابط)
        /// </summary>
        public LawCategory? Category { get; set; }

        /// <summary>
        /// وصف القانون (اختياري)
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// كلمات مفتاحية (اختياري - لو فاضي هيتم استخراجها من الرابط)
        /// </summary>
        [StringLength(500)]
        public string? SearchKeywords { get; set; }
    }
}