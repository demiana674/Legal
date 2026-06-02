// ============================================================
// 3. DTO - AppointmentResponseDto.cs
// ============================================================
// LegalMateAI.DTOs/ReadDTO/AppointmentResponseDto.cs
using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AppointmentResponseDto
    {
        public Guid Id { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public string AppointmentType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string DateFormatted { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public AppointmentStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public Guid UserId { get; set; }
        public Guid LawyerId { get; set; }
        public Guid BranchId { get; set; }
        public UserBriefDto User { get; set; } = new UserBriefDto();
        public LawyerBriefDto Lawyer { get; set; } = new LawyerBriefDto();
    }

    
}