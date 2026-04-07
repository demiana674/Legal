using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class RiskAssessmentDto
    {
        public Guid Id { get; set; }
        public string RiskType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RiskLevel Level { get; set; }
        public string LevelName => Level.ToString();
        public string LevelColor => Level switch
        {
            RiskLevel.Low => "#4CAF50",
            RiskLevel.Medium => "#FFC107",
            RiskLevel.High => "#FF9800",
            RiskLevel.Critical => "#F44336",
            _ => "#9E9E9E"
        };
        public string? Suggestion { get; set; }
    }
}

