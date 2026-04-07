using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class DocumentAnalysis
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        public string? ExtractedText { get; set; }
        public string? Summary { get; set; }
        public string? Result { get; set; }
        public string? AnalysisData { get; set; }
        public AnalysisStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? SuggestedLawyerId { get; set; }
        
        public ICollection<ClauseAnalysis> Clauses { get; set; } = new List<ClauseAnalysis>();
        public ICollection<RiskAssessment> Risks { get; set; } = new List<RiskAssessment>();
    }
}