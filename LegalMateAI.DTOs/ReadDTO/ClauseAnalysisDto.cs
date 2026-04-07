using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ClauseAnalysisDto
    {
        public Guid Id { get; set; }
        public string ClauseTitle { get; set; } = string.Empty;
        public string ClauseText { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string? Interpretation { get; set; }
    }
}


