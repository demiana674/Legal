namespace LegalMateAI.DTOs.ReadDTO
{
    public class KpiCardDto
    {
        public string Name { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double PreviousValue { get; set; }
        public double GrowthRate { get; set; }
        public string Trend { get; set; } = "up";
        public string Color { get; set; } = "green";
    }
}