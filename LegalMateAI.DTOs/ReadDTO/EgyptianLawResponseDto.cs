using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 1. عرض القانون
    public class EgyptianLawResponseDto
    {
        public int Id { get; set; }
        public string LawNumber { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public string? TitleEn { get; set; }
        public int Year { get; set; }
        public LawCategory Category { get; set; }
        public string CategoryName => Category switch
        {
            LawCategory.Civil => "مدني",
            LawCategory.Criminal => "جنائي",
            LawCategory.Commercial => "تجاري",
            LawCategory.Labor => "عمل",
            LawCategory.Family => "أحوال شخصية",
            LawCategory.Administrative => "إداري",
            LawCategory.Constitutional => "دستوري",
            LawCategory.Tax => "ضرائب",
            LawCategory.RealEstate => "عقاري",
            LawCategory.Procedure => "إجراءات",
            LawCategory.Maritime => "بحري",
            LawCategory.Investment => "استثمار",
            _ => Category.ToString()
        };
        public LawStatus Status { get; set; }
        public string StatusName => Status switch
        {
            LawStatus.Active => "ساري",
            LawStatus.Amended => "معدل",
            LawStatus.Repealed => "ملغي",
            LawStatus.Draft => "مسودة",
            LawStatus.UnderReview => "قيد المراجعة",
            _ => Status.ToString()
        };
        public string? Description { get; set; }
        public DateTime PublishedAt { get; set; }
        public string PublishedAtFormatted => PublishedAt.ToString("dd MMM yyyy");
        public DateTime? LastAmendedAt { get; set; }
        public int ViewCount { get; set; }
        public int ArticlesCount { get; set; }
        
        public List<LawArticleBriefDto> Articles { get; set; } = new();
        public List<LawAmendmentBriefDto> Amendments { get; set; } = new();
        public string[]? Keywords { get; set; }
    }
}

