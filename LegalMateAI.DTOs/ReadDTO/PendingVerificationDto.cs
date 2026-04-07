using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class PendingVerificationDto
    {
        public Guid LawyerId { get; set; }
        public string LawyerName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string SubmittedAtFormatted => SubmittedAt.ToString("dd MMM yyyy");
        public string DocumentUrl { get; set; } = string.Empty;
    }
}


