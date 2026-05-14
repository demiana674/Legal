using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMateAI.Domain.Entities
{
    [Table("DataWarehouseFacts")]
    public class DataWarehouseFact
    {
        [Key]
        public Guid Id { get; set; }

        public int TimeDimId { get; set; }
        public int UserDimId { get; set; }
        public int LawyerDimId { get; set; }
        public int CaseDimId { get; set; }
        public int ContractDimId { get; set; }
        public int DocumentDimId { get; set; }
        public int LocationDimId { get; set; }

        public int CaseCount { get; set; }
        public int ContractCount { get; set; }
        public int DocumentCount { get; set; }
        public int AppointmentCount { get; set; }
        public decimal TotalFees { get; set; }
        public double AverageRating { get; set; }
        public int SuccessRate { get; set; }
        public int ResponseTimeMinutes { get; set; }
        public int UserSatisfactionScore { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        [ForeignKey("TimeDimId")]
        public virtual TimeDimension? TimeDim { get; set; }
        
        [ForeignKey("UserDimId")]
        public virtual UserDimension? UserDim { get; set; }
        
        [ForeignKey("CaseDimId")]
        public virtual CaseDimension? CaseDim { get; set; }
        
        [ForeignKey("LocationDimId")]
        public virtual LocationDimension? LocationDim { get; set; }
    }
}