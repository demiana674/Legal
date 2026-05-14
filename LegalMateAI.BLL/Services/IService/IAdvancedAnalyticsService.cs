// LegalMateAI.BLL/Services/IService/IAdvancedAnalyticsService.cs
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    /// <summary>
    /// خدمات التحليلات المتقدمة - دمج الـ Data Warehouse مع الـ ML
    /// </summary>
    public interface IAdvancedAnalyticsService
    {
        /// <summary>
        /// تحليل كامل لأداء النظام (Dashboard شامل)
        /// </summary>
        Task<FullAnalyticsReportDto> GetFullAnalyticsReportAsync(DateTime? fromDate = null, DateTime? toDate = null);
        
        /// <summary>
        /// تحليل تنبؤي للمخاطر المستقبلية
        /// </summary>
        Task<RiskForecastDto> GetRiskForecastAsync(int months = 3);
        
        /// <summary>
        /// توصيات ذكية للمستخدمين بناءً على سلوكهم
        /// </summary>
        Task<List<SmartRecommendationDto>> GetSmartRecommendationsAsync(Guid userId, int topK = 5);
        
        /// <summary>
        /// تحليل أداء المحامين المتقدم
        /// </summary>
        Task<LawyerPerformanceAnalyticsDto> GetLawyerPerformanceAnalyticsAsync(Guid? lawyerId = null);
        
        /// <summary>
        /// تجزئة السوق والعملاء
        /// </summary>
        Task<MarketSegmentationDto> GetMarketSegmentationAsync();
        
        /// <summary>
        /// لوحة تحكم حية (Real-time)
        /// </summary>
        Task<RealTimeDashboardDto> GetRealTimeDashboardAsync();
    }

    public class FullAnalyticsReportDto
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public ExecutiveDashboardDto ExecutiveDashboard { get; set; } = new();
        public List<AnomalyDto> RecentAnomalies { get; set; } = new();
        public List<AssociationRuleDto> TopRules { get; set; } = new();
        public ClusteringResultDto UserSegments { get; set; } = new();
        public ForecastDto CaseForecast { get; set; } = new();
        public Dictionary<string, object> SummaryStats { get; set; } = new();
    }

    public class RiskForecastDto
    {
        public List<MonthlyRiskDto> MonthlyForecast { get; set; } = new();
        public double OverallRiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    public class MonthlyRiskDto
    {
        public DateTime Month { get; set; }
        public double PredictedRiskScore { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public List<string> TopRiskFactors { get; set; } = new();
    }

    public class SmartRecommendationDto
    {
        public string RecommendationType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public class LawyerPerformanceAnalyticsDto
    {
        public Guid? LawyerId { get; set; }
        public string LawyerName { get; set; } = string.Empty;
        
        // KPIs
        public double SuccessRate { get; set; }
        public double AvgResponseTime { get; set; }
        public double ClientSatisfaction { get; set; }
        public int TotalCases { get; set; }
        public int TotalRevenue { get; set; }
        
        // Trends
        public List<TrendDto> PerformanceTrend { get; set; } = new();
        public List<TrendDto> CaseVolumeTrend { get; set; } = new();
        
        // Benchmarks
        public double VsSystemAvg { get; set; }
        public string Rank { get; set; } = string.Empty;
        public int RankPosition { get; set; }
        
        // Recommendations
        public List<string> ImprovementAreas { get; set; } = new();
    }

    public class MarketSegmentationDto
    {
        public List<SegmentDto> Segments { get; set; } = new();
        public Dictionary<string, double> SegmentDistribution { get; set; } = new();
        public List<string> Insights { get; set; } = new();
    }

    public class SegmentDto
    {
        public string Name { get; set; } = string.Empty;
        public int Size { get; set; }
        public double Percentage { get; set; }
        public List<string> Characteristics { get; set; } = new();
        public double AverageValue { get; set; }
        public string RecommendedStrategy { get; set; } = string.Empty;
    }

    public class RealTimeDashboardDto
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int ActiveUsersNow { get; set; }
        public int ActiveLawyersNow { get; set; }
        public int CasesToday { get; set; }
        public int AppointmentsToday { get; set; }
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
        public Dictionary<string, int> LiveStats { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public DateTime Time { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityName { get; set; }
    }
}