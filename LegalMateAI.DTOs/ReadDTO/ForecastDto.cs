using System.Collections.Generic;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ForecastDto
    {
        public List<TrendDto> Historical { get; set; } = new();
        public List<TrendDto> Predicted { get; set; } = new();
        public double Confidence { get; set; }
        public string Method { get; set; } = string.Empty;
    }
}