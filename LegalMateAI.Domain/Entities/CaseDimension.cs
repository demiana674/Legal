using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMateAI.Domain.Entities
{
    [Table("CaseDimensions")]
    public class CaseDimension
    {
        [Key]
        public int Id { get; set; }
        public Guid CaseId { get; set; }
        public string CaseType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public bool IsResolved { get; set; }
        public string Complexity { get; set; } = string.Empty;
    }
}