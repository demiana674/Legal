using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerBriefDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public double Rating { get; set; }
         public string? Initials { get; set; } = string.Empty;
        // public string? Initials => FullName.Length >= 2 ? FullName.Substring(0, 2) : FullName;
    }
}


