// LegalMateAI.Domain/Entities/Case.cs
using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class Case
    {
        public Guid Id { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // العلاقات
        public Guid ClientId { get; set; }
        public User Client { get; set; } = null!;
        
        public Guid? LawyerId { get; set; }
        public LawyerProfile? Lawyer { get; set; }
        
        // معلومات القضية
        public string? Court { get; set; }
        public DateTime? NextHearingDate { get; set; }
        public CaseStatus Status { get; set; } = CaseStatus.Pending;
        public CasePriority Priority { get; set; } = CasePriority.Medium;
        public string? CaseType { get; set; }
        
        // تواريخ
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        
        // الملاحظات والمستندات
        public ICollection<CaseDocument> Documents { get; set; } = new List<CaseDocument>();
        public ICollection<CaseNote> Notes { get; set; } = new List<CaseNote>();
    }
}