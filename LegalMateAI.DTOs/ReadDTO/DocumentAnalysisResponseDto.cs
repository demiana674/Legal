using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    // 5. Document Analysis Response
    public class DocumentAnalysisResponseDto
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public AnalysisStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<ClauseAnalysisDto> Clauses { get; set; } = new();
        public List<RiskAssessmentDto> Risks { get; set; } = new();
        public List<LawyerSuggestionDto> SuggestedLawyers { get; set; } = new();
    }
}
