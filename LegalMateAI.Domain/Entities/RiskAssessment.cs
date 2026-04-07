using LegalMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class RiskAssessment
    {
        public Guid Id { get; set; }
        public Guid AnalysisId { get; set; }
        public DocumentAnalysis Analysis { get; set; } = null!;
        
        public string RiskType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RiskLevel Level { get; set; }
        public string? Suggestion { get; set; }
    }
}