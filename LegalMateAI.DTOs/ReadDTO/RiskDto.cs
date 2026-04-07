using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
// LegalMateAI.DTOs/ReadDTO/RiskDto.cs
namespace LegalMateAI.DTOs.ReadDTO
{
    public class RiskDto
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string? Suggestion { get; set; }
    }
}