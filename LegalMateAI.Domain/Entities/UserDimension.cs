using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMateAI.Domain.Entities
{
    [Table("UserDimensions")]
    public class UserDimension
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
        public int AgeGroup { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public int AccountAgeDays { get; set; }
        public bool IsVerified { get; set; }
        public int TotalInteractions { get; set; }
        public decimal LifetimeValue { get; set; }
    }
}