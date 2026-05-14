using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AnomalyDto
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string AnomalyType { get; set; } = string.Empty;
        public double AnomalyScore { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsConfirmed { get; set; }
    }
}