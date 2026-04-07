using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class RescheduleResponseDto
    {
        public Guid Id { get; set; }
        public DateTime OldDate { get; set; }
        public string OldTime { get; set; } = string.Empty;
        public DateTime NewDate { get; set; }
        public string NewTime { get; set; } = string.Empty;
        public RescheduleInitiator InitiatedBy { get; set; }
        public RescheduleStatus Status { get; set; }
        public string? Reason { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}

