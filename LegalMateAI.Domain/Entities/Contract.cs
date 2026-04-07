using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class Contract
    {
        public Guid Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid? LawyerId { get; set; }
        public LawyerProfile? Lawyer { get; set; }
        
        public string Title { get; set; } = string.Empty;
        public ContractType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string? FileFormat { get; set; }
        public string? PartyName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Value { get; set; }
        public decimal? MonetaryValue { get; set; }
        public ContractStatus Status { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public DateTime? SignedAt { get; set; }
        public bool IsGeneratedByAI { get; set; }
        
        public ICollection<ContractClause> Clauses { get; set; } = new List<ContractClause>();
    }
}