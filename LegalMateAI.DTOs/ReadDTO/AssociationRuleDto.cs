namespace LegalMateAI.DTOs.ReadDTO
{
    public class AssociationRuleDto
    {
        public string Antecedent { get; set; } = string.Empty;
        public string Consequent { get; set; } = string.Empty;
        public double Support { get; set; }
        public double Confidence { get; set; }
        public double Lift { get; set; }
        public string Interpretation { get; set; } = string.Empty;
    }
}