using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class TrendDto
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public double? Forecast { get; set; }
    }
}