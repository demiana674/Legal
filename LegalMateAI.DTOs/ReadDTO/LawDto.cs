// LegalMateAI.DTOs/ReadDTO/LawDto.cs
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
        public string CategoryName => GetCategoryName(Category);
        public string? Description { get; set; }
        public string PdfFileUrl { get; set; } = string.Empty;
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
        
        private static string GetCategoryName(LawCategory category)
        {
            return category switch
            {
                LawCategory.Constitutional => "دستوري",
                LawCategory.Civil => "مدني",
                LawCategory.Commercial => "تجاري",
                LawCategory.Criminal => "جنائي",
                LawCategory.Family => "أحوال شخصية",
                LawCategory.Labor => "عمل",
                LawCategory.Tax => "ضريبي",
                LawCategory.Administrative => "إداري",
                LawCategory.RealEstate => "عقاري",
                LawCategory.Investment => "استثمار",
                LawCategory.Maritime => "بحري",
                LawCategory.Procedure => "إجراءات",
                LawCategory.Financial => "مالي",
                LawCategory.Social => "اجتماعي",
                LawCategory.Educational => "تعليم",
                LawCategory.Economic => "اقتصادي",
                LawCategory.Other => "أخرى",
                _ => category.ToString()
            };
        }
    }
}