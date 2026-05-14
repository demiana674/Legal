namespace LegalMateAI.DTOs.ReadDTO
{
    public class SequencePatternDto
    {
        public string Pattern { get; set; } = string.Empty;
        public int Support { get; set; }
        public double Confidence { get; set; }
        public double AverageDuration { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }
}