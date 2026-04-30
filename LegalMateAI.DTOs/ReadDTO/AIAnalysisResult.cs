// LegalMateAI.DTOs/ReadDTO/AIAnalysisResult.cs
namespace LegalMateAI.DTOs.ReadDTO
{
    public class AIAnalysisResult
    {
        public string Summary { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public List<AIClause> Clauses { get; set; } = new();
        public List<AIRisk> Risks { get; set; } = new();
        public bool IsFallback { get; set; }
    }

    public class AIClause
    {
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string Importance { get; set; } = "Medium";
         public string Interpretation { get; set; } = string.Empty;
    }

    public class AIRisk
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
         public Domain.Enums.RiskLevel RiskLevel { get; set; }
        public string Suggestion { get; set; } = string.Empty;
    }

    public class QuickAnalysisResult
    {
        public string RiskSummary { get; set; } = string.Empty;
        public string ClauseSummary { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public List<string> DetectedRisks { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public bool IsFallback { get; set; }
    }
}