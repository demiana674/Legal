// LegalMateAI.BLL/Services/Service/AdvancedAnalyticsService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.Service
{
    public class AdvancedAnalyticsService : IAdvancedAnalyticsService
    {
        private readonly LegalMateDbContext _context;
        private readonly IDataWarehouseService _warehouseService;
        private readonly IDataMiningService _dataMiningService;
        private readonly IRecommendationService _recommendationService;
        private readonly ILogger<AdvancedAnalyticsService> _logger;

        public AdvancedAnalyticsService(
            LegalMateDbContext context,
            IDataWarehouseService warehouseService,
            IDataMiningService dataMiningService,
            IRecommendationService recommendationService,
            ILogger<AdvancedAnalyticsService> logger)
        {
            _context = context;
            _warehouseService = warehouseService;
            _dataMiningService = dataMiningService;
            _recommendationService = recommendationService;
            _logger = logger;
        }

        public async Task<FullAnalyticsReportDto> GetFullAnalyticsReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            _logger.LogInformation("Generating full analytics report");
            
            var report = new FullAnalyticsReportDto
            {
                GeneratedAt = DateTime.UtcNow,
                ExecutiveDashboard = await _warehouseService.GetExecutiveDashboardAsync(),
                RecentAnomalies = await _dataMiningService.DetectAnomalousActivitiesAsync(fromDate ?? DateTime.UtcNow.AddDays(-30)),
                TopRules = await _dataMiningService.FindCasePatternsAsync(minConfidence: 0.6),
                UserSegments = await _dataMiningService.ClusterUsersAsync(k: 4),
                CaseForecast = await _warehouseService.GetForecastAsync("cases", 6),
                SummaryStats = await GetSummaryStatisticsAsync()
            };
            
            return report;
        }

        public async Task<RiskForecastDto> GetRiskForecastAsync(int months = 3)
        {
            _logger.LogInformation($"Generating risk forecast for {months} months");
            
            var forecast = new RiskForecastDto();
            var monthlyForecast = new List<MonthlyRiskDto>();
            
            // Get historical case data
            var historicalCases = await _context.Cases
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    HighRiskCount = g.Count(c => c.Priority == CasePriority.Urgent || c.Priority == CasePriority.High),
                    TotalCount = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();
            
            if (!historicalCases.Any())
                return forecast;
            
            // Calculate risk ratio
            var riskRatios = historicalCases.Select(x => (double)x.HighRiskCount / x.TotalCount).ToList();
            
            // Simple forecasting (moving average)
            var windowSize = Math.Min(3, riskRatios.Count);
            var lastValues = riskRatios.Skip(Math.Max(0, riskRatios.Count - windowSize)).ToList();
            var avgRisk = lastValues.Any() ? lastValues.Average() : 0.3;
            
            var lastDate = historicalCases.Last();
            var currentDate = new DateTime(lastDate.Year, lastDate.Month, 1);
            
            for (int i = 1; i <= months; i++)
            {
                var forecastDate = currentDate.AddMonths(i);
                var predictedRisk = avgRisk + (i * 0.01); // Slight upward trend
                predictedRisk = Math.Min(predictedRisk, 0.8);
                
                monthlyForecast.Add(new MonthlyRiskDto
                {
                    Month = forecastDate,
                    PredictedRiskScore = Math.Round(predictedRisk * 100, 1),
                    LowerBound = Math.Round(Math.Max(0, predictedRisk - 0.1) * 100, 1),
                    UpperBound = Math.Round(Math.Min(1, predictedRisk + 0.1) * 100, 1),
                    TopRiskFactors = GetTopRiskFactors(forecastDate)
                });
            }
            
            forecast.MonthlyForecast = monthlyForecast;
            forecast.OverallRiskScore = Math.Round(avgRisk * 100, 1);
            forecast.RiskLevel = avgRisk switch
            {
                > 0.5 => "عالي",
                > 0.3 => "متوسط",
                _ => "منخفض"
            };
            forecast.Recommendations = GetRiskRecommendations(avgRisk);
            
            return forecast;
        }

        public async Task<List<SmartRecommendationDto>> GetSmartRecommendationsAsync(Guid userId, int topK = 5)
        {
            _logger.LogInformation($"Getting smart recommendations for user {userId}");
            
            var recommendations = new List<SmartRecommendationDto>();
            
            // 1. Get user's case history
            var userCases = await _context.Cases
                .Where(c => c.ClientId == userId)
                .ToListAsync();
            
            // 2. Get lawyer recommendations based on user history
            var lawyerRecommendations = await _recommendationService.RecommendByUserHistoryAsync(userId, topK);
            
            if (lawyerRecommendations.Any())
            {
                recommendations.Add(new SmartRecommendationDto
                {
                    RecommendationType = "LawyerMatch",
                    Title = "محامون موصى بهم",
                    Description = "بناءً على تقييماتك السابقة وتخصصات القضايا",
                    Confidence = 0.85,
                    Data = new Dictionary<string, object>
                    {
                        ["lawyers"] = lawyerRecommendations.Take(3).ToList()
                    }
                });
            }
            
            // 3. Check if user has pending cases without lawyers
            var casesWithoutLawyer = userCases.Count(c => c.LawyerId == null && c.Status != CaseStatus.Completed);
            if (casesWithoutLawyer > 0)
            {
                recommendations.Add(new SmartRecommendationDto
                {
                    RecommendationType = "ActionRequired",
                    Title = "قضايا بدون محامي",
                    Description = $"لديك {casesWithoutLawyer} قضية بدون محامي معين",
                    Confidence = 0.95,
                    Data = new Dictionary<string, object>
                    {
                        ["count"] = casesWithoutLawyer
                    }
                });
            }
            
            // 4. Document analysis recommendation
            var unanalyzedDocs = await _context.Documents
                .Where(d => d.UserId == userId && !_context.DocumentAnalyses.Any(a => a.DocumentId == d.Id))
                .CountAsync();
            
            if (unanalyzedDocs > 0)
            {
                recommendations.Add(new SmartRecommendationDto
                {
                    RecommendationType = "FeatureSuggestion",
                    Title = "تحليل المستندات الذكي",
                    Description = $"لديك {unanalyzedDocs} مستند لم يتم تحليلها بعد",
                    Confidence = 0.9,
                    Data = new Dictionary<string, object>
                    {
                        ["documents_count"] = unanalyzedDocs
                    }
                });
            }
            
            // 5. Time-based recommendations
            var lastActive = await _context.AdminLogs
                .Where(l => l.ActorId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => l.Timestamp)
                .FirstOrDefaultAsync();
            
            if (lastActive < DateTime.UtcNow.AddDays(-30))
            {
                recommendations.Add(new SmartRecommendationDto
                {
                    RecommendationType = "Engagement",
                    Title = "عودة إلى المنصة",
                    Description = "لم تقم بزيارة المنصة منذ فترة، هناك تحديثات جديدة",
                    Confidence = 0.7
                });
            }
            
            return recommendations.Take(topK).ToList();
        }

        public async Task<LawyerPerformanceAnalyticsDto> GetLawyerPerformanceAnalyticsAsync(Guid? lawyerId = null)
        {
            _logger.LogInformation($"Getting lawyer performance analytics for lawyer {(lawyerId.HasValue ? lawyerId.ToString() : "ALL")}");
            
            var result = new LawyerPerformanceAnalyticsDto();
            
            IQueryable<Case> casesQuery = _context.Cases;
            IQueryable<LawyerReview> reviewsQuery = _context.LawyerReviews;
            
            if (lawyerId.HasValue)
            {
                casesQuery = casesQuery.Where(c => c.LawyerId == lawyerId.Value);
                reviewsQuery = reviewsQuery.Where(r => r.LawyerId == lawyerId.Value);
                
                var lawyer = await _context.LawyerProfiles
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Id == lawyerId.Value);
                
                if (lawyer != null)
                {
                    result.LawyerId = lawyerId;
                    result.LawyerName = lawyer.User.FullName;
                }
            }
            
            // Calculate KPIs
            var cases = await casesQuery.ToListAsync();
            var completedCases = cases.Count(c => c.Status == CaseStatus.Completed);
            var rejectedCases = cases.Count(c => c.Status == CaseStatus.Rejected);
            
            result.SuccessRate = cases.Any() ? Math.Round((double)completedCases / cases.Count * 100, 1) : 0;
            result.TotalCases = cases.Count;
            
            var reviews = await reviewsQuery.ToListAsync();
            result.ClientSatisfaction = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;
            
            // Calculate response time
            var appointments = await _context.Appointments
                .Where(a => a.Lawyer.UserId == lawyerId || a.LawyerId == (lawyerId ?? Guid.Empty))
                .ToListAsync();
            
            var confirmedAppointments = appointments.Where(a => a.ConfirmedAt.HasValue).ToList();
            result.AvgResponseTime = confirmedAppointments.Any() 
                ? Math.Round(confirmedAppointments.Average(a => (a.ConfirmedAt.Value - a.RequestedAt).TotalMinutes), 0)
                : 0;
            
            // Trends
            result.PerformanceTrend = cases
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new TrendDto
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Value = g.Count()
                })
                .ToList();
            
            // Benchmark against system average
            var allLawyersSuccess = await _context.Cases
                .Where(c => c.LawyerId != null)
                .GroupBy(c => c.LawyerId)
                .Select(g => new { Success = g.Count(c => c.Status == CaseStatus.Completed), Total = g.Count() })
                .ToListAsync();
            
            var avgSuccessRate = allLawyersSuccess.Any() 
                ? allLawyersSuccess.Average(x => x.Total > 0 ? (double)x.Success / x.Total * 100 : 0)
                : 0;
            
            result.VsSystemAvg = Math.Round(result.SuccessRate - avgSuccessRate, 1);
            
            // Rank
            var lawyerRanks = allLawyersSuccess
                .Select(x => x.Total > 0 ? (double)x.Success / x.Total : 0)
                .OrderByDescending(x => x)
                .ToList();
            
            var currentRate = cases.Any() ? (double)completedCases / cases.Count : 0;
            result.RankPosition = lawyerRanks.FindIndex(r => r <= currentRate) + 1;
            result.Rank = result.RankPosition <= 3 ? "متميز" : result.RankPosition <= 10 ? "جيد" : "متوسط";
            
            // Improvement areas
            result.ImprovementAreas = GetImprovementAreas(result);
            
            return result;
        }

        public async Task<MarketSegmentationDto> GetMarketSegmentationAsync()
        {
            _logger.LogInformation("Generating market segmentation");
            
            var segmentation = new MarketSegmentationDto();
            var clusters = await _dataMiningService.ClusterUsersAsync(k: 4);
            
            var totalUsers = clusters.Clusters.Sum(c => c.Size);
            
            var segments = new List<SegmentDto>();
            var segmentNames = new[] { "العملاء الجدد", "العملاء النشطون", "العملاء المتميزون", "العملاء غير النشطين" };
            
            for (int i = 0; i < clusters.Clusters.Count && i < segmentNames.Length; i++)
            {
                var cluster = clusters.Clusters[i];
                segments.Add(new SegmentDto
                {
                    Name = segmentNames[i],
                    Size = cluster.Size,
                    Percentage = totalUsers > 0 ? Math.Round((double)cluster.Size / totalUsers * 100, 1) : 0,
                    Characteristics = cluster.Characteristics,
                    AverageValue = cluster.Centroids.GetValueOrDefault("avg_value", 0),
                    RecommendedStrategy = GetSegmentStrategy(segmentNames[i])
                });
            }
            
            segmentation.Segments = segments;
            segmentation.SegmentDistribution = segments.ToDictionary(s => s.Name, s => s.Percentage);
            segmentation.Insights = GetMarketInsights(segments);
            
            return segmentation;
        }

        public async Task<RealTimeDashboardDto> GetRealTimeDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            
            var dashboard = new RealTimeDashboardDto
            {
                Timestamp = now,
                ActiveUsersNow = await _context.AdminLogs
                    .Where(l => l.Timestamp > now.AddMinutes(-5))
                    .Select(l => l.ActorId)
                    .Distinct()
                    .CountAsync(),
                ActiveLawyersNow = await _context.AdminLogs
                    .Where(l => l.Timestamp > now.AddMinutes(-5) && l.ActorRole == "Lawyer")
                    .Select(l => l.ActorId)
                    .Distinct()
                    .CountAsync(),
                CasesToday = await _context.Cases.CountAsync(c => c.CreatedAt.Date == today),
                AppointmentsToday = await _context.Appointments.CountAsync(a => a.RequestedAt.Date == today),
                RecentActivities = await GetRecentActivitiesAsync(10),
                LiveStats = new Dictionary<string, int>
                {
                    ["total_users"] = await _context.Users.CountAsync(),
                    ["total_lawyers"] = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer),
                    ["pending_verifications"] = await _context.LawyerProfiles.CountAsync(l => l.VerificationStatus == LawyerVerificationStatus.Pending),
                    ["documents_uploaded_today"] = await _context.Documents.CountAsync(d => d.UploadedAt.Date == today)
                }
            };
            
            return dashboard;
        }

        // ========== Private Helper Methods ==========

        private async Task<Dictionary<string, object>> GetSummaryStatisticsAsync()
        {
            return new Dictionary<string, object>
            {
                ["total_users"] = await _context.Users.CountAsync(),
                ["total_lawyers"] = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer),
                ["total_cases"] = await _context.Cases.CountAsync(),
                ["total_contracts"] = await _context.Contracts.CountAsync(),
                ["total_appointments"] = await _context.Appointments.CountAsync(),
                ["total_documents"] = await _context.Documents.CountAsync(),
                ["success_rate_overall"] = await CalculateOverallSuccessRateAsync(),
                ["avg_user_rating"] = await CalculateAvgUserRatingAsync()
            };
        }

        private async Task<double> CalculateOverallSuccessRateAsync()
        {
            var completedCases = await _context.Cases.CountAsync(c => c.Status == CaseStatus.Completed);
            var totalCases = await _context.Cases.CountAsync();
            return totalCases > 0 ? Math.Round((double)completedCases / totalCases * 100, 1) : 0;
        }

        private async Task<double> CalculateAvgUserRatingAsync()
        {
            var reviews = await _context.LawyerReviews.ToListAsync();
            return reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;
        }

        private List<string> GetTopRiskFactors(DateTime date)
        {
            // In production, this would analyze actual data
            return new List<string>
            {
                "قضايا بدون محامي معين",
                "تأخير في تقديم المستندات",
                "قضايا ذات أولوية عالية",
                "قضايا قديمة (أكثر من 6 أشهر)"
            };
        }

        private List<string> GetRiskRecommendations(double riskScore)
        {
            var recommendations = new List<string>();
            
            if (riskScore > 0.5)
            {
                recommendations.Add("زيادة عدد المحامين المتخصصين في القضايا عالية المخاطر");
                recommendations.Add("تفعيل نظام التذكير التلقائي للمواعيد الهامة");
            }
            
            recommendations.Add("مراجعة القضايا المعلقة أسبوعياً");
            recommendations.Add("تقديم دورات تدريبية للمحامين حول إدارة المخاطر");
            
            return recommendations;
        }

        private List<string> GetImprovementAreas(LawyerPerformanceAnalyticsDto performance)
        {
            var areas = new List<string>();
            
            if (performance.SuccessRate < 70)
                areas.Add("تحسين نسبة نجاح القضايا - التركيز على القضايا ذات فرص النجاح العالية");
            
            if (performance.AvgResponseTime > 120)
                areas.Add("تحسين وقت الاستجابة - الرد على طلبات المواعيد بشكل أسرع");
            
            if (performance.ClientSatisfaction < 4)
                areas.Add("تحسين رضا العملاء - طلب التقييم بعد كل قضية ناجحة");
            
            if (performance.TotalCases < 10)
                areas.Add("زيادة عدد القضايا - الترويج للخدمات عبر المنصة");
            
            if (!areas.Any())
                areas.Add("استمرار الأداء المتميز - الحفاظ على جودة الخدمات");
            
            return areas;
        }

        private string GetSegmentStrategy(string segmentName)
        {
            return segmentName switch
            {
                "العملاء الجدد" => "تقديم تجربة مجانية أو خصم على أول استشارة",
                "العملاء النشطون" => "برنامج ولاء مع نقاط مكافآت",
                "العملاء المتميزون" => "خدمة VIP ودعم مخصص",
                "العملاء غير النشطين" => "حملات إعادة تنشيط عبر البريد الإلكتروني",
                _ => "تحسين تجربة المستخدم العامة"
            };
        }

        private List<string> GetMarketInsights(List<SegmentDto> segments)
        {
            var insights = new List<string>();
            
            var largestSegment = segments.OrderByDescending(s => s.Size).FirstOrDefault();
            if (largestSegment != null)
            {
                insights.Add($"أكبر شريحة من المستخدمين هي '{largestSegment.Name}' بنسبة {largestSegment.Percentage}%");
            }
            
            var highValueSegments = segments.Where(s => s.AverageValue > 1000).ToList();
            if (highValueSegments.Any())
            {
                insights.Add($"الشرائح ذات القيمة العالية: {string.Join(", ", highValueSegments.Select(s => s.Name))}");
            }
            
            insights.Add("توصية: استهداف العملاء الجدد بعروض ترحيبية");
            insights.Add("فرصة: زيادة التسويق في المناطق ذات النشاط المنخفض");
            
            return insights;
        }

        private async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int count)
        {
            var logs = await _context.AdminLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
            
            return logs.Select(l => new RecentActivityDto
            {
                Time = l.Timestamp,
                UserName = l.ActorName ?? "غير معروف",
                Action = l.Action.ToString(),
                EntityType = l.TargetType,
                EntityName = l.TargetType
            }).ToList();
        }
    }
}