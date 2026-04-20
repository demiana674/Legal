// LegalMateAI.DTOs/ReadDTO/PredefinedContractTemplateDto.cs
using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class PredefinedContractTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? Description { get; set; }
        public ContractType ContractType { get; set; }
        
        public string ContractTypeName => ContractType switch
        {
            ContractType.Rental => "عقد إيجار",
            ContractType.Employment => "عقد عمل",
            ContractType.Sale => "عقد بيع",
            ContractType.Service => "عقد خدمات",
            ContractType.Partnership => "عقد شراكة",
            ContractType.PowerOfAttorney => "وكالة قانونية",
            ContractType.Settlement => "عقد صلح وتسوية",
            ContractType.Other => "عقد آخر",
            _ => ContractType.ToString()
        };
        
        public string ContractTypeIcon => ContractType switch
        {
            ContractType.Rental => "🏠",
            ContractType.Employment => "💼",
            ContractType.Sale => "💰",
            ContractType.Service => "📋",
            ContractType.Partnership => "🤝",
            ContractType.PowerOfAttorney => "⚖️",
            ContractType.Settlement => "🕊️",
            _ => "📄"
        };
        
        public string FileFormat { get; set; } = "pdf";
        public string FileUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public List<string> RequiredFields { get; set; } = new();
        public List<string> SearchKeywords { get; set; } = new();
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public int DownloadCount { get; set; }
        public int UsageCount { get; set; }
        public double? Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => CreatedAt.ToString("dd MMM yyyy");
        public DateTime? UpdatedAt { get; set; }
        public string AdminName { get; set; } = string.Empty;
        
        /// <summary>
        /// نسبة التطابق مع البحث (تستخدم في الـ Frontend)
        /// </summary>
        public double? MatchScore { get; set; }
    }
}