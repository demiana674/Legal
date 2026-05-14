using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMateAI.Domain.Entities
{
    [Table("RecommendationLogs")]
    public class RecommendationLog
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? RecommendedLawyerId { get; set; }
        public string? SearchContext { get; set; }
        public string? DetectedSpecialization { get; set; }
        public int ResultsCount { get; set; }
        public bool WasSelected { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}