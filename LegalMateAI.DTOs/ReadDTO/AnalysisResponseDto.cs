using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

// LegalMateAI.DTOs/ReadDTO/AnalysisResponseDto.cs
namespace LegalMateAI.DTOs.ReadDTO
{
    public class AnalysisResponseDto
    {
        public string Summary { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public List<ClauseDto> Clauses { get; set; } = new();
        public List<RiskDto> Risks { get; set; } = new();
        public List<LawyerSuggestionDto> SuggestedLawyers { get; set; } = new();
    }
}