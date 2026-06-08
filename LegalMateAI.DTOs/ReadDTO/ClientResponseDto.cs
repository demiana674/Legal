using System;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ClientResponseDto
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientInitials { get; set; } = string.Empty;
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        public DateTime ClientSince { get; set; }
        public string ClientSinceFormatted => ClientSince.ToString("MMMM yyyy");
        
        public Guid CaseId { get; set; }
        public string CaseTitle { get; set; } = string.Empty;
        public string CaseType { get; set; } = string.Empty;
        public string CaseNumber { get; set; } = string.Empty;
        public string? CaseDescription { get; set; }
        public int CaseProgress { get; set; }
        public CaseStatus CaseStatus { get; set; }
        public CasePriority CasePriority { get; set; }
        public string? Court { get; set; }
        public DateTime? NextHearingDate { get; set; }
        public bool IsUrgent { get; set; }
        
        public int ContractsCount { get; set; }
        public int AppointmentsCount { get; set; }
        public Appointment? LastAppointment { get; set; }
    }
}