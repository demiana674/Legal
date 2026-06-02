using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.Service
{
    public class DataWarehouseService : IDataWarehouseService
    {
        private readonly LegalMateDbContext _context;
        private readonly ILogger<DataWarehouseService> _logger;

        public DataWarehouseService(
            LegalMateDbContext context,
            ILogger<DataWarehouseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> RunETLAsync()
        {
            _logger.LogInformation("Starting full ETL process...");
            
            try
            {
                var cases = await _context.Cases.ToListAsync();
                var contracts = await _context.Contracts.ToListAsync();
                var appointments = await _context.Appointments.ToListAsync();
                var documents = await _context.Documents.ToListAsync();
                var users = await _context.Users.ToListAsync();
                var lawyers = await _context.LawyerProfiles
                    .Include(l => l.User)
                    .Include(l => l.Reviews)
                    .ToListAsync();

                var facts = new List<DataWarehouseFact>();
                
                var dateGroups = cases
                    .Select(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                    .Union(contracts.Select(c => new { c.CreatedAt.Year, c.CreatedAt.Month }))
                    .Union(appointments.Select(a => new { a.RequestedAt.Year, a.RequestedAt.Month }))
                    .Distinct()
                    .ToList();

                foreach (var dateGroup in dateGroups)
                {
                    var timeDim = GetOrCreateTimeDimension(dateGroup.Year, dateGroup.Month);
                    
                    var monthCases = cases.Where(c => c.CreatedAt.Year == dateGroup.Year && c.CreatedAt.Month == dateGroup.Month).ToList();
                    var monthContracts = contracts.Where(c => c.CreatedAt.Year == dateGroup.Year && c.CreatedAt.Month == dateGroup.Month).ToList();
                    var monthAppointments = appointments.Where(a => a.RequestedAt.Year == dateGroup.Year && a.RequestedAt.Month == dateGroup.Month).ToList();
                    
                    var fact = new DataWarehouseFact
                    {
                        Id = Guid.NewGuid(),
                        TimeDimId = timeDim.Id,
                        CaseCount = monthCases.Count,
                        ContractCount = monthContracts.Count,
                        AppointmentCount = monthAppointments.Count,
                        DocumentCount = documents.Count(d => d.UploadedAt.Year == dateGroup.Year && d.UploadedAt.Month == dateGroup.Month),
                        TotalFees = monthContracts.Sum(c => c.MonetaryValue ?? 0),
                        AverageRating = lawyers.Any() ? lawyers.Average(l => l.Reviews?.Average(r => r.Rating) ?? 0) : 0,
                        SuccessRate = CalculateSuccessRate(monthCases),
                        ResponseTimeMinutes = CalculateAvgResponseTime(monthAppointments),
                        UserSatisfactionScore = CalculateSatisfactionScore(users),
                        RecordedAt = DateTime.UtcNow
                    };
                    
                    facts.Add(fact);
                }

                await _context.DataWarehouseFacts.AddRangeAsync(facts);
                
                var cutoffDate = DateTime.UtcNow.AddMonths(-24);
                var oldFacts = await _context.DataWarehouseFacts
                    .Where(f => f.RecordedAt < cutoffDate)
                    .ToListAsync();
                _context.DataWarehouseFacts.RemoveRange(oldFacts);
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"ETL completed. Added {facts.Count} facts, removed {oldFacts.Count} old facts.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ETL process failed");
                return false;
            }
        }

        public async Task<bool> IncrementalLoadAsync(DateTime fromDate)
        {
            _logger.LogInformation($"Starting incremental load from {fromDate:yyyy-MM-dd}");
            
            try
            {
                var newCases = await _context.Cases
                    .Where(c => c.CreatedAt >= fromDate)
                    .ToListAsync();
                    
                var newContracts = await _context.Contracts
                    .Where(c => c.CreatedAt >= fromDate)
                    .ToListAsync();

                if (!newCases.Any() && !newContracts.Any())
                {
                    _logger.LogInformation("No new data to load");
                    return true;
                }

                foreach (var caseGroup in newCases.GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month }))
                {
                    var timeDim = GetOrCreateTimeDimension(caseGroup.Key.Year, caseGroup.Key.Month);
                    
                    var existingFact = await _context.DataWarehouseFacts
                        .FirstOrDefaultAsync(f => f.TimeDimId == timeDim.Id);
                    
                    if (existingFact != null)
                    {
                        existingFact.CaseCount += caseGroup.Count();
                        existingFact.ContractCount += newContracts.Count(c => c.CreatedAt.Year == caseGroup.Key.Year && c.CreatedAt.Month == caseGroup.Key.Month);
                    }
                    else
                    {
                        _context.DataWarehouseFacts.Add(new DataWarehouseFact
                        {
                            Id = Guid.NewGuid(),
                            TimeDimId = timeDim.Id,
                            CaseCount = caseGroup.Count(),
                            ContractCount = newContracts.Count,
                            RecordedAt = DateTime.UtcNow
                        });
                    }
                }
                
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Incremental load completed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Incremental load failed");
                return false;
            }
        }

        public async Task<bool> BuildCubesAsync()
        {
            _logger.LogInformation("Building OLAP cubes...");
            
            try
            {
                var monthlyStats = await _context.DataWarehouseFacts
                    .Include(f => f.TimeDim)
                    .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Month })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        TotalCases = g.Sum(x => x.CaseCount),
                        TotalContracts = g.Sum(x => x.ContractCount),
                        TotalAppointments = g.Sum(x => x.AppointmentCount),
                        AvgRating = g.Average(x => x.AverageRating),
                        TotalFees = g.Sum(x => x.TotalFees)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();
                
                _logger.LogInformation($"Cubes built successfully with {monthlyStats.Count} aggregated records");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cube building failed");
                return false;
            }
        }

        public async Task<OlapResultDto> GetCaseAnalysisAsync(
            string? timeDimension = null,
            string? caseType = null,
            string? location = null,
            string[]? measures = null)
        {
            var result = new OlapResultDto();
            
            var query = _context.DataWarehouseFacts
                .Include(f => f.TimeDim)
                .Include(f => f.CaseDim)
                .Include(f => f.LocationDim)
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(caseType))
                query = query.Where(f => f.CaseDim != null && f.CaseDim.CaseType == caseType);
            
            if (!string.IsNullOrEmpty(location))
                query = query.Where(f => f.LocationDim != null && f.LocationDim.GovernorateName == location);
            
            var data = new List<Dictionary<string, object>>();
            
            if (timeDimension?.ToLower() == "year")
            {
                var grouped = await query
                    .GroupBy(f => f.TimeDim!.Year)
                    .Select(g => new { Period = g.Key, TotalCases = g.Sum(x => x.CaseCount), TotalContracts = g.Sum(x => x.ContractCount), AvgRating = g.Average(x => x.AverageRating), SuccessRate = g.Average(x => x.SuccessRate), TotalFees = g.Sum(x => x.TotalFees) })
                    .ToListAsync();
                
                foreach (var g in grouped)
                {
                    var dict = new Dictionary<string, object>();
                    dict["period"] = g.Period;
                    dict["total_cases"] = g.TotalCases;
                    dict["total_contracts"] = g.TotalContracts;
                    dict["avg_rating"] = Math.Round(g.AvgRating, 2);
                    dict["success_rate"] = Math.Round(g.SuccessRate, 2);
                    dict["total_fees"] = g.TotalFees;
                    data.Add(dict);
                }
            }
            else if (timeDimension?.ToLower() == "quarter")
            {
                var grouped = await query
                    .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Quarter })
                    .Select(g => new { Period = $"{g.Key.Year}-Q{g.Key.Quarter}", TotalCases = g.Sum(x => x.CaseCount), TotalContracts = g.Sum(x => x.ContractCount), AvgRating = g.Average(x => x.AverageRating), SuccessRate = g.Average(x => x.SuccessRate), TotalFees = g.Sum(x => x.TotalFees) })
                    .ToListAsync();
                
                foreach (var g in grouped)
                {
                    var dict = new Dictionary<string, object>();
                    dict["period"] = g.Period;
                    dict["total_cases"] = g.TotalCases;
                    dict["total_contracts"] = g.TotalContracts;
                    dict["avg_rating"] = Math.Round(g.AvgRating, 2);
                    dict["success_rate"] = Math.Round(g.SuccessRate, 2);
                    dict["total_fees"] = g.TotalFees;
                    data.Add(dict);
                }
            }
            else
            {
                var grouped = await query
                    .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Month })
                    .Select(g => new { Period = $"{g.Key.Year}-{g.Key.Month}", TotalCases = g.Sum(x => x.CaseCount), TotalContracts = g.Sum(x => x.ContractCount), AvgRating = g.Average(x => x.AverageRating), SuccessRate = g.Average(x => x.SuccessRate), TotalFees = g.Sum(x => x.TotalFees) })
                    .ToListAsync();
                
                foreach (var g in grouped)
                {
                    var dict = new Dictionary<string, object>();
                    dict["period"] = g.Period;
                    dict["total_cases"] = g.TotalCases;
                    dict["total_contracts"] = g.TotalContracts;
                    dict["avg_rating"] = Math.Round(g.AvgRating, 2);
                    dict["success_rate"] = Math.Round(g.SuccessRate, 2);
                    dict["total_fees"] = g.TotalFees;
                    data.Add(dict);
                }
            }
            
            result.Data = data;
            result.Dimensions = new List<string> { timeDimension ?? "month" };
            result.Measures = new List<string> { "total_cases", "total_contracts", "avg_rating", "success_rate", "total_fees" };
            result.Aggregates = new Dictionary<string, object>
            {
                ["total_cases_all"] = data.Sum(d => Convert.ToDouble(d["total_cases"])),
                ["total_fees_all"] = data.Sum(d => Convert.ToDecimal(d["total_fees"]))
            };
            result.GeneratedAt = DateTime.UtcNow;
            
            return result;
        }

        public async Task<OlapResultDto> GetLawyerPerformanceAsync(
            int? lawyerId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.DataWarehouseFacts
                .Include(f => f.TimeDim)
                .AsQueryable();
            
            var performance = new List<Dictionary<string, object>>();
            
            var grouped = await query
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalCases = g.Sum(x => x.CaseCount),
                    TotalContracts = g.Sum(x => x.ContractCount),
                    SuccessRate = g.Average(x => x.SuccessRate),
                    AvgRating = g.Average(x => x.AverageRating),
                    AvgResponseTime = g.Average(x => x.ResponseTimeMinutes),
                    Satisfaction = g.Average(x => x.UserSatisfactionScore)
                })
                .FirstOrDefaultAsync();
            
            if (grouped != null)
            {
                var dict = new Dictionary<string, object>();
                dict["lawyer_id"] = lawyerId ?? 0;
                dict["total_cases"] = grouped.TotalCases;
                dict["total_contracts"] = grouped.TotalContracts;
                dict["success_rate"] = Math.Round(grouped.SuccessRate, 2);
                dict["avg_rating"] = Math.Round(grouped.AvgRating, 2);
                dict["avg_response_time"] = Math.Round(grouped.AvgResponseTime, 0);
                dict["satisfaction"] = Math.Round(grouped.Satisfaction, 2);
                performance.Add(dict);
            }
            
            return new OlapResultDto
            {
                Data = performance,
                Dimensions = new List<string> { "lawyer_id" },
                Measures = new List<string> { "total_cases", "success_rate", "avg_rating", "avg_response_time", "satisfaction" },
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<OlapResultDto> GetUserBehaviorAsync(
            string userSegment = "all",
            string timeGranularity = "month")
        {
            var query = _context.DataWarehouseFacts
                .Include(f => f.TimeDim)
                .Include(f => f.UserDim)
                .AsQueryable();
            
            var behavior = new List<Dictionary<string, object>>();
            
            var grouped = await query
                .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, TotalInteractions = g.Sum(x => x.CaseCount + x.ContractCount + x.AppointmentCount), AvgSatisfaction = g.Average(x => x.UserSatisfactionScore) })
                .ToListAsync();
            
            foreach (var g in grouped)
            {
                var dict = new Dictionary<string, object>();
                dict["period"] = $"{g.Year}-{g.Month}";
                dict["active_users"] = 0;
                dict["total_interactions"] = g.TotalInteractions;
                dict["avg_satisfaction"] = Math.Round(g.AvgSatisfaction, 2);
                behavior.Add(dict);
            }
            
            return new OlapResultDto
            {
                Data = behavior,
                Dimensions = new List<string> { "period" },
                Measures = new List<string> { "active_users", "total_interactions", "avg_satisfaction" },
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync()
        {
            var today = DateTime.UtcNow;
            var lastMonth = today.AddMonths(-1);
            
            var currentData = await _context.DataWarehouseFacts
                .Include(f => f.TimeDim)
                .Where(f => f.TimeDim!.Year == today.Year && f.TimeDim.Month == today.Month)
                .ToListAsync();
            
            var previousData = await _context.DataWarehouseFacts
                .Include(f => f.TimeDim)
                .Where(f => f.TimeDim!.Year == lastMonth.Year && f.TimeDim.Month == lastMonth.Month)
                .ToListAsync();
            
            var totalUsers = await _context.Users.CountAsync();
            var activeLawyers = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer && u.IsActive);
            
            var topLawyers = await GetTopLawyersAsync(5);
            var trends = await GetTrendsAsync("cases", 12);
            var geoDistribution = await GetGeoDistributionAsync();
            
            return new ExecutiveDashboardDto
            {
                TotalUsers = new KpiCardDto
                {
                    Name = "إجمالي المستخدمين",
                    CurrentValue = totalUsers,
                    PreviousValue = await _context.Users.CountAsync(u => u.CreatedAt < lastMonth),
                    GrowthRate = CalculateGrowthRate(totalUsers, await _context.Users.CountAsync(u => u.CreatedAt < lastMonth)),
                    Trend = "up",
                    Color = "green"
                },
                ActiveLawyers = new KpiCardDto
                {
                    Name = "المحاميون النشطون",
                    CurrentValue = activeLawyers,
                    PreviousValue = await _context.Users.CountAsync(u => u.Role == UserRole.Lawyer && u.IsActive && u.CreatedAt < lastMonth),
                    GrowthRate = 5.2,
                    Color = "blue"
                },
                MonthlyCases = new KpiCardDto
                {
                    Name = "القضايا هذا الشهر",
                    CurrentValue = currentData.Sum(d => d.CaseCount),
                    PreviousValue = previousData.Sum(d => d.CaseCount),
                    GrowthRate = CalculateGrowthRate(currentData.Sum(d => d.CaseCount), previousData.Sum(d => d.CaseCount)),
                    Color = "purple"
                },
                SuccessRate = new KpiCardDto
                {
                    Name = "نسبة النجاح",
                    CurrentValue = currentData.Any() ? currentData.Average(d => d.SuccessRate) : 0,
                    PreviousValue = previousData.Any() ? previousData.Average(d => d.SuccessRate) : 0,
                    GrowthRate = (currentData.Any() ? currentData.Average(d => d.SuccessRate) : 0) - (previousData.Any() ? previousData.Average(d => d.SuccessRate) : 0),
                    Color = "green"
                },
                AvgResponseTime = new KpiCardDto
                {
                    Name = "متوسط وقت الاستجابة (دقيقة)",
                    CurrentValue = currentData.Any() ? currentData.Average(d => d.ResponseTimeMinutes) : 0,
                    PreviousValue = previousData.Any() ? previousData.Average(d => d.ResponseTimeMinutes) : 0,
                    GrowthRate = -((currentData.Any() ? currentData.Average(d => d.ResponseTimeMinutes) : 0) - (previousData.Any() ? previousData.Average(d => d.ResponseTimeMinutes) : 0)),
                    Trend = (currentData.Any() ? currentData.Average(d => d.ResponseTimeMinutes) : 0) < (previousData.Any() ? previousData.Average(d => d.ResponseTimeMinutes) : 0) ? "down" : "up",
                    Color = "orange"
                },
                Revenue = new KpiCardDto
                {
                    Name = "الإيرادات",
                    CurrentValue = (double)currentData.Sum(d => d.TotalFees),
                    PreviousValue = (double)previousData.Sum(d => d.TotalFees),
                    GrowthRate = CalculateGrowthRate((double)currentData.Sum(d => d.TotalFees), (double)previousData.Sum(d => d.TotalFees)),
                    Color = "gold"
                },
                Trends = trends.Select(t => new TrendChartDto
                {
                    Label = t.Date.ToString("MMM yyyy"),
                    Data = new List<double> { t.Value },
                    Color = "blue"
                }).ToList(),
                GeoDistribution = geoDistribution,
                TopLawyers = topLawyers
            };
        }

        public async Task<List<TrendDto>> GetTrendsAsync(string metric, int months = 12)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);
            
            var trends = new List<TrendDto>();
            
            var rawData = await _context.DataWarehouseFacts
                .Include(f => f.TimeDim)
                .Where(f => f.TimeDim!.Year >= startDate.Year && 
                           (f.TimeDim.Year > startDate.Year || f.TimeDim.Month >= startDate.Month))
                .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Cases = g.Sum(x => x.CaseCount),
                    Contracts = g.Sum(x => x.ContractCount),
                    Revenue = g.Sum(x => x.TotalFees),
                    Count = g.Count()
                })
                .ToListAsync();
            
            foreach (var item in rawData)
            {
                double value = 0;
                if (metric == "cases")
                    value = item.Cases;
                else if (metric == "contracts")
                    value = item.Contracts;
                else if (metric == "revenue")
                    value = (double)item.Revenue;
                else if (metric == "users")
                    value = item.Count;
                else
                    value = item.Cases;
                    
                trends.Add(new TrendDto
                {
                    Date = new DateTime(item.Year, item.Month, 1),
                    Value = value
                });
            }
            
            for (int i = 0; i < 3; i++)
            {
                var lastValues = trends.Skip(Math.Max(0, trends.Count - 3)).Select(d => d.Value).ToList();
                var forecast = lastValues.Any() ? lastValues.Average() : 0;
                trends.Add(new TrendDto
                {
                    Date = trends.Any() ? trends.Last().Date.AddMonths(1) : DateTime.UtcNow,
                    Value = forecast,
                    Forecast = forecast
                });
            }
            
            return trends;
        }

        public async Task<ForecastDto> GetForecastAsync(string metric, int periods = 6)
        {
            var historical = await GetTrendsAsync(metric, 24);
            
            if (!historical.Any())
            {
                return new ForecastDto
                {
                    Historical = new List<TrendDto>(),
                    Predicted = new List<TrendDto>(),
                    Confidence = 0,
                    Method = "No data available"
                };
            }
            
            var xValues = Enumerable.Range(0, historical.Count).Select(i => (double)i).ToArray();
            var yValues = historical.Select(h => h.Value).ToArray();
            
            var (slope, intercept) = SimpleLinearRegression(xValues, yValues);
            
            var predicted = new List<TrendDto>();
            for (int i = 0; i < periods; i++)
            {
                var nextIndex = historical.Count + i;
                var predictedValue = slope * nextIndex + intercept;
                predicted.Add(new TrendDto
                {
                    Date = historical.Last().Date.AddMonths(i + 1),
                    Value = Math.Max(0, predictedValue),
                    Forecast = Math.Max(0, predictedValue)
                });
            }
            
            var ssRes = 0.0;
            var ssTot = 0.0;
            var avgY = yValues.Average();
            for (int i = 0; i < yValues.Length; i++)
            {
                var predictedY = slope * xValues[i] + intercept;
                ssRes += Math.Pow(yValues[i] - predictedY, 2);
                ssTot += Math.Pow(yValues[i] - avgY, 2);
            }
            var rSquared = ssTot > 0 ? 1 - (ssRes / ssTot) : 0;
            
            return new ForecastDto
            {
                Historical = historical,
                Predicted = predicted,
                Confidence = Math.Round(rSquared * 100, 1),
                Method = "Linear Regression"
            };
        }

        // ========== Private Helper Methods ==========

        private TimeDimension GetOrCreateTimeDimension(int year, int month)
        {
            var existing = _context.TimeDimensions
                .FirstOrDefault(t => t.Year == year && t.Month == month);
            
            if (existing != null)
                return existing;
            
            var newDim = new TimeDimension
            {
                Id = (_context.TimeDimensions.Max(t => (int?)t.Id) ?? 0) + 1,
                Year = year,
                Quarter = (month - 1) / 3 + 1,
                Month = month,
                Week = GetWeekOfMonth(year, month),
                Day = 1,
                MonthName = GetMonthName(month),
                IsWeekend = false,
                Season = GetSeason(month)
            };
            
            _context.TimeDimensions.Add(newDim);
            _context.SaveChanges();
            return newDim;
        }

        private int GetWeekOfMonth(int year, int month)
        {
            var firstDay = new DateTime(year, month, 1);
            return (firstDay.DayOfYear / 7) + 1;
        }

        private string GetMonthName(int month) => month switch
        {
            1 => "يناير", 2 => "فبراير", 3 => "مارس", 4 => "أبريل",
            5 => "مايو", 6 => "يونيو", 7 => "يوليو", 8 => "أغسطس",
            9 => "سبتمبر", 10 => "أكتوبر", 11 => "نوفمبر", 12 => "ديسمبر",
            _ => month.ToString()
        };

        private string GetSeason(int month) => month switch
        {
            12 or 1 or 2 => "شتاء",
            3 or 4 or 5 => "ربيع",
            6 or 7 or 8 => "صيف",
            9 or 10 or 11 => "خريف",
            _ => "غير معروف"
        };

        private int CalculateSuccessRate(List<Case> cases)
        {
            if (!cases.Any()) return 0;
            var completed = cases.Count(c => c.Status == CaseStatus.Completed);
            return (int)Math.Round((double)completed / cases.Count * 100);
        }

        private int CalculateAvgResponseTime(List<Appointment> appointments)
        {
            if (!appointments.Any()) return 0;
            var responseTimes = appointments
                .Where(a => a.ConfirmedAt.HasValue)
                .Select(a => (a.ConfirmedAt.Value - a.RequestedAt).TotalMinutes);
            return responseTimes.Any() ? (int)responseTimes.Average() : 0;
        }

        private int CalculateSatisfactionScore(List<User> users)
        {
            return 75;
        }

        private async Task<List<LawyerLeaderboardDto>> GetTopLawyersAsync(int limit)
        {
            return await _context.LawyerProfiles
                .Include(lp => lp.User)
                .Include(lp => lp.Reviews)
                // .Where(lp => lp.VerificationStatus == LawyerVerificationStatus.Active)
                // ✅ بعد التعديل
               .Where(lp => lp.User.Status == AccountStatus.Active)
                .Select(lp => new LawyerLeaderboardDto
                {
                    LawyerId = lp.Id,
                    LawyerName = lp.User.FullName,
                    ProfilePicture = lp.User.ProfilePicture,
                    TotalCases = _context.Cases.Count(c => c.LawyerId == lp.Id),
                    SuccessRate = 75,
                    AverageRating = lp.Reviews.Any() ? lp.Reviews.Average(r => r.Rating) : 0,
                    TotalReviews = lp.Reviews.Count,
                    Revenue = 0
                })
                .OrderByDescending(l => l.AverageRating)
                .Take(limit)
                .ToListAsync();
        }

        private async Task<List<GeoDistributionDto>> GetGeoDistributionAsync()
        {
            var distribution = await _context.Cases
                .Include(c => c.Client!)
                .ThenInclude(c => c.UserProfile!)
                .Where(c => c.Client != null && c.Client.UserProfile != null && c.Client.UserProfile.Governorate != null)
                .GroupBy(c => c.Client.UserProfile.Governorate.Name)
                .Select(g => new GeoDistributionDto
                {
                    Governorate = g.Key ?? "غير محدد",
                    Count = g.Count()
                })
                .ToListAsync();
            
            var total = distribution.Sum(d => d.Count);
            foreach (var d in distribution)
            {
                d.Percentage = total > 0 ? Math.Round((double)d.Count / total * 100, 1) : 0;
            }
            
            return distribution.OrderByDescending(d => d.Count).ToList();
        }

        private double CalculateGrowthRate(double current, double previous)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return Math.Round((current - previous) / previous * 100, 1);
        }

        private (double slope, double intercept) SimpleLinearRegression(double[] x, double[] y)
        {
            if (x.Length < 2 || y.Length < 2)
                return (0, y.FirstOrDefault());
                
            var n = x.Length;
            var sumX = x.Sum();
            var sumY = y.Sum();
            var sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
            var sumX2 = x.Select(xi => xi * xi).Sum();
            
            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;
            
            return (slope, intercept);
        }
    }
}