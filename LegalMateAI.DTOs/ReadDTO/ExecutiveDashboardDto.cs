using System.Collections.Generic;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ExecutiveDashboardDto
    {
        public KpiCardDto TotalUsers { get; set; } = new();
        public KpiCardDto ActiveLawyers { get; set; } = new();
        public KpiCardDto MonthlyCases { get; set; } = new();
        public KpiCardDto SuccessRate { get; set; } = new();
        public KpiCardDto AvgResponseTime { get; set; } = new();
        public KpiCardDto Revenue { get; set; } = new();
        public List<TrendChartDto> Trends { get; set; } = new();
        public List<GeoDistributionDto> GeoDistribution { get; set; } = new();
        public List<LawyerLeaderboardDto> TopLawyers { get; set; } = new();
    }
}