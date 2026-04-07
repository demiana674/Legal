using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ClientBriefDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Cases { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}

