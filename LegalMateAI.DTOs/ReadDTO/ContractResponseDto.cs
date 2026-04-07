using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    // 7. Contract Response
    public class ContractResponseDto
    {
        public Guid Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ContractType Type { get; set; }
        public string TypeName => Type switch
        {
            ContractType.Rental => "عقد إيجار",
            ContractType.Employment => "عقد عمل",
            ContractType.Sale => "عقد بيع",
            ContractType.Service => "عقد خدمات",
            ContractType.Partnership => "عقد شراكة",
            ContractType.PowerOfAttorney => "وكالة قانونية",
            ContractType.Settlement => "عقد تسوية",
            _ => Type.ToString()
        };
        public string Content { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string? PartyName { get; set; }
        public DateTime StartDate { get; set; }
        public string StartDateFormatted => StartDate.ToString("dd MMM yyyy");
        public DateTime? EndDate { get; set; }
        public string? EndDateFormatted => EndDate?.ToString("dd MMM yyyy");
        public string? Value { get; set; }
        public ContractStatus Status { get; set; }
        public string StatusName => Status switch
        {
            ContractStatus.Draft => "مسودة",
            ContractStatus.PendingSignature => "انتظار التوقيع",
            ContractStatus.Active => "نشط",
            ContractStatus.Expired => "منتهي",
            ContractStatus.Terminated => "ملغي",
            _ => Status.ToString()
        };
        public string StatusColor => Status switch
        {
            ContractStatus.Draft => "#9E9E9E",
            ContractStatus.PendingSignature => "#F5A623",
            ContractStatus.Active => "#4CAF50",
            ContractStatus.Expired => "#F44336",
            ContractStatus.Terminated => "#9E9E9E",
            _ => "#9E9E9E"
        };
        public int ProgressPercentage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public DateTime? SignedAt { get; set; }
        public bool IsGeneratedByAI { get; set; }
        
        public UserBriefDto User { get; set; } = null!;
        public LawyerBriefDto? Lawyer { get; set; }
        public List<ContractClauseDto> Clauses { get; set; } = new();
    }
}


