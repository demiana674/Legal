using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
namespace LegalMateAI.DTOs.CreateDTO
{
    // 1. إنشاء قانون جديد
    public class CreateEgyptianLawDto
    {
        [Required(ErrorMessage = "رقم القانون مطلوب")]
        public string LawNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "عنوان القانون مطلوب")]
        public string TitleAr { get; set; } = string.Empty;

        public string? TitleEn { get; set; }

        [Required(ErrorMessage = "سنة الإصدار مطلوبة")]
        [Range(1800, 2100)]
        public int Year { get; set; }

        [Required(ErrorMessage = "التصنيف مطلوب")]
        public LawCategory Category { get; set; }

        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime PublishedAt { get; set; }

        public string? SourceUrl { get; set; }

        public string[]? Keywords { get; set; }
    }

    // 2. إنشاء مادة قانونية
    public class CreateLawArticleDto
    {
        [Required]
        public int LawId { get; set; }

        [Required(ErrorMessage = "رقم المادة مطلوب")]
        public int ArticleNumber { get; set; }

        public string? Title { get; set; }

        [Required(ErrorMessage = "نص المادة مطلوب")]
        public string Content { get; set; } = string.Empty;

        public string? Summary { get; set; }

        public List<CreateClauseDto>? Clauses { get; set; }
    }

    // 3. إنشاء فقرة
    public class CreateClauseDto
    {
        [Required]
        public string ClauseNumber { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public int Order { get; set; }
    }

    // 4. إنشاء تعديل قانوني
    public class CreateLawAmendmentDto
    {
        [Required]
        public int LawId { get; set; }

        [Required]
        public string AmendmentNumber { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime AmendmentDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; }

        public string Description { get; set; } = string.Empty;

        public int[]? AffectedArticles { get; set; }
    }

    // 5. إنشاء تفسير قانوني
    public class CreateLawInterpretationDto
    {
        public int? LawId { get; set; }
        public int? ArticleId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public InterpretationSource Source { get; set; }

        public string? SourceReference { get; set; }
    }
}