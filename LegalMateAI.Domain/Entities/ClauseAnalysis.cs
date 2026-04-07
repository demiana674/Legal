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
        public class ClauseAnalysis
    {
        public Guid Id { get; set; }
        public Guid AnalysisId { get; set; }
        public DocumentAnalysis Analysis { get; set; } = null!;
        
        public string ClauseTitle { get; set; } = string.Empty;
        public string ClauseText { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string? Interpretation { get; set; }
    }
}