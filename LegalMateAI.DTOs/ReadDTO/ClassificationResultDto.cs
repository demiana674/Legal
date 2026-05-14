using System.Collections.Generic;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ClassificationResultDto
    {
        public string PredictedClass { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public Dictionary<string, double> Probabilities { get; set; } = new();
        public List<string> TopFeatures { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
    }
}