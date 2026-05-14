using System.Collections.Generic;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class TrendChartDto
    {
        public string Label { get; set; } = string.Empty;
        public List<double> Data { get; set; } = new();
        public string Color { get; set; } = "blue";
    }
}