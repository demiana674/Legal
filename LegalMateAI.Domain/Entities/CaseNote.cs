// LegalMateAI.Domain/Entities/CaseNote.cs
using System;

namespace LegalMateAI.Domain.Entities
{
    public class CaseNote
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public Case Case { get; set; } = null!;
        
        public string Content { get; set; } = string.Empty;
        public Guid WrittenBy { get; set; }  // UserId (Lawyer or Admin)
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsPrivate { get; set; }  // ملاحظة خاصة للمحامي فقط
    }
}