// LegalMateAI.DTOs/CreateDTO/CreateLawDto.cs
using System.ComponentModel.DataAnnotations;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateLawDto
    {
        [Required(ErrorMessage = "اسم القانون مطلوب")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? LawNumber { get; set; }

        [Range(1800, 2100)]
        public int? Year { get; set; }

        [Required(ErrorMessage = "تصنيف القانون مطلوب")]
        public LawCategory Category { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Url(ErrorMessage = "رابط المصدر غير صحيح")]
        public string? SourceUrl { get; set; }

        [StringLength(500)]
        public string? SearchKeywords { get; set; }

        /// <summary>
        /// ملف PDF (اختياري - لو الأدمن عايز يرفع ملف)
        /// </summary>
        public IFormFile? PdfFile { get; set; }

        /// <summary>
        /// رابط PDF خارجي (اختياري)
        /// </summary>
        [Url]
        public string? PdfFileUrl { get; set; }
    }
}