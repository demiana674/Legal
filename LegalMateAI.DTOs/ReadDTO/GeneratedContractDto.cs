// LegalMateAI.DTOs/ReadDTO/GeneratedContractDto.cs
using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class GeneratedContractDto
    {
        public Guid Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public ContractType ContractType { get; set; }
        public string ContractTypeName => ContractType switch
        {
            ContractType.Rental => "عقد إيجار",
            ContractType.Employment => "عقد عمل",
            ContractType.Sale => "عقد بيع",
            ContractType.Service => "عقد خدمات",
            ContractType.Partnership => "عقد شراكة",
            _ => ContractType.ToString()
        };
        public Dictionary<string, string> FilledData { get; set; } = new();
        public string PdfDownloadUrl { get; set; } = string.Empty;
        public ContractStatus Status { get; set; }
        public string StatusName => Status switch
        {
            ContractStatus.Draft => "مسودة",
            ContractStatus.PendingSignature => "في انتظار التوقيع",
            ContractStatus.Active => "نشط",
            ContractStatus.Expired => "منتهي",
            ContractStatus.Terminated => "ملغي",
            _ => Status.ToString()
        };
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => CreatedAt.ToString("dd MMM yyyy HH:mm");
        public DateTime? ExpiresAt { get; set; }
        public UserBriefDto User { get; set; } = null!;
        public LawyerBriefDto? Lawyer { get; set; }
    }
}