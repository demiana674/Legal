using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IDataWarehouseService
    {
        Task<bool> RunETLAsync();
        Task<bool> IncrementalLoadAsync(DateTime fromDate);
        Task<bool> BuildCubesAsync();
        
        Task<OlapResultDto> GetCaseAnalysisAsync(
            string? timeDimension = null,
            string? caseType = null,
            string? location = null,
            string[]? measures = null);
        
        Task<OlapResultDto> GetLawyerPerformanceAsync(
            int? lawyerId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);
        
        Task<OlapResultDto> GetUserBehaviorAsync(
            string userSegment = "all",
            string timeGranularity = "month");
        
        Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync();
        Task<List<TrendDto>> GetTrendsAsync(string metric, int months = 12);
        Task<ForecastDto> GetForecastAsync(string metric, int periods = 6);
    }
}