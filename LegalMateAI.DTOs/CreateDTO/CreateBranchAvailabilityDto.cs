// LegalMateAI.DTOs/CreateDTO/CreateBranchAvailabilityDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class CreateBranchAvailabilityDto
    {
        [Required]
        public DayOfWeek DayOfWeek { get; set; }
        
        [Required]
        public TimeSpan StartTime { get; set; }
        
        [Required]
        public TimeSpan EndTime { get; set; }
        
        [Range(15, 240)]
        public int SlotDurationMinutes { get; set; } = 60;
        
        public bool IsAvailable { get; set; } = true;
    }
}