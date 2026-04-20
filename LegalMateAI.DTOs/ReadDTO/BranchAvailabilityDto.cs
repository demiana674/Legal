// LegalMateAI.DTOs/ReadDTO/BranchAvailabilityDto.cs
using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class BranchAvailabilityDto
    {
        public Guid Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int SlotDurationMinutes { get; set; }
        public bool IsAvailable { get; set; }
    }
}