// LegalMateAI.API/Controllers/AnalyticsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using System.Security.Claims;

namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IDataWarehouseService _warehouseService;
        private readonly IDataMiningService _dataMiningService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            IDataWarehouseService warehouseService,
            IDataMiningService dataMiningService,
            ILogger<AnalyticsController> logger)
        {
            _warehouseService = warehouseService;
            _dataMiningService = dataMiningService;
            _logger = logger;
        }

        private Guid GetAdminId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) 
                ?? User.FindFirst("id") 
                ?? User.FindFirst("sub");
            
            if (claim == null)
                throw new UnauthorizedAccessException("Admin ID not found");
            
            return Guid.Parse(claim.Value);
        }

        // ========== ETL Operations ==========

        /// <summary>
        /// تشغيل عملية ETL كاملة لنقل البيانات إلى Data Warehouse
        /// </summary>
        [HttpPost("etl/run")]
        public async Task<IActionResult> RunETL()
        {
            _logger.LogInformation($"Admin {GetAdminId()} triggered ETL");
            var result = await _warehouseService.RunETLAsync();
            
            return Ok(new { 
                success = result, 
                message = result ? "تم تشغيل ETL بنجاح" : "فشل تشغيل ETL",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// تشغيل تحميل تدريجي للبيانات الجديدة فقط
        /// </summary>
        [HttpPost("etl/incremental")]
        public async Task<IActionResult> RunIncrementalLoad([FromQuery] DateTime fromDate)
        {
            var result = await _warehouseService.IncrementalLoadAsync(fromDate);
            return Ok(new { success = result });
        }

        /// <summary>
        /// بناء مكعبات OLAP للتحليل السريع
        /// </summary>
        [HttpPost("etl/build-cubes")]
        public async Task<IActionResult> BuildCubes()
        {
            var result = await _warehouseService.BuildCubesAsync();
            return Ok(new { success = result });
        }

        // ========== OLAP Queries ==========

        /// <summary>
        /// تحليل القضايا متعدد الأبعاد (Slice and Dice)
        /// </summary>
        [HttpGet("olap/cases")]
        public async Task<IActionResult> GetCaseAnalysis(
            [FromQuery] string? timeDimension = null,
            [FromQuery] string? caseType = null,
            [FromQuery] string? location = null)
        {
            var result = await _warehouseService.GetCaseAnalysisAsync(timeDimension, caseType, location);
            return Ok(result);
        }

        /// <summary>
        /// تحليل أداء المحامين
        /// </summary>
        [HttpGet("olap/lawyers")]
        public async Task<IActionResult> GetLawyerPerformance(
            [FromQuery] int? lawyerId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _warehouseService.GetLawyerPerformanceAsync(lawyerId, fromDate, toDate);
            return Ok(result);
        }

        /// <summary>
        /// تحليل سلوك المستخدمين
        /// </summary>
        [HttpGet("olap/users")]
        public async Task<IActionResult> GetUserBehavior(
            [FromQuery] string userSegment = "all",
            [FromQuery] string timeGranularity = "month")
        {
            var result = await _warehouseService.GetUserBehaviorAsync(userSegment, timeGranularity);
            return Ok(result);
        }

        // ========== Dashboards ==========

        /// <summary>
        /// لوحة التحكم التنفيذية (KPIs والرسوم البيانية)
        /// </summary>
        [HttpGet("dashboard/executive")]
        public async Task<IActionResult> GetExecutiveDashboard()
        {
            var dashboard = await _warehouseService.GetExecutiveDashboardAsync();
            return Ok(dashboard);
        }

        /// <summary>
        /// الاتجاهات الزمنية لمؤشر معين
        /// </summary>
        [HttpGet("trends/{metric}")]
        public async Task<IActionResult> GetTrends(string metric, [FromQuery] int months = 12)
        {
            var trends = await _warehouseService.GetTrendsAsync(metric, months);
            return Ok(new { metric, months, data = trends });
        }

        /// <summary>
        /// التوقعات المستقبلية لمؤشر معين
        /// </summary>
        [HttpGet("forecast/{metric}")]
        public async Task<IActionResult> GetForecast(string metric, [FromQuery] int periods = 6)
        {
            var forecast = await _warehouseService.GetForecastAsync(metric, periods);
            return Ok(forecast);
        }

        // ========== Data Mining - Association Rules ==========

        /// <summary>
        /// اكتشاف قواعد الارتباط بين عناصر القضايا
        /// </summary>
        [HttpGet("mining/association-rules")]
        public async Task<IActionResult> GetAssociationRules(
            [FromQuery] double minSupport = 0.01,
            [FromQuery] double minConfidence = 0.5)
        {
            var rules = await _dataMiningService.FindCasePatternsAsync(minSupport, minConfidence);
            return Ok(new { count = rules.Count, rules });
        }

        /// <summary>
        /// أنماط العلاقات بين العملاء والمحامين
        /// </summary>
        [HttpGet("mining/lawyer-client-patterns")]
        public async Task<IActionResult> GetLawyerClientPatterns()
        {
            var patterns = await _dataMiningService.FindLawyerClientPatternsAsync();
            return Ok(patterns);
        }

        // ========== Data Mining - Clustering ==========

        /// <summary>
        /// تجميع المستخدمين إلى مجموعات متشابهة
        /// </summary>
        [HttpGet("mining/clusters/users")]
        public async Task<IActionResult> GetUserClusters([FromQuery] int k = 5)
        {
            var clusters = await _dataMiningService.ClusterUsersAsync(k);
            return Ok(clusters);
        }

        /// <summary>
        /// تجميع القضايا إلى مجموعات متشابهة
        /// </summary>
        [HttpGet("mining/clusters/cases")]
        public async Task<IActionResult> GetCaseClusters([FromQuery] int k = 5)
        {
            var clusters = await _dataMiningService.ClusterCasesAsync(k);
            return Ok(clusters);
        }

        /// <summary>
        /// تجميع المستندات حسب مستوى الخطورة
        /// </summary>
        [HttpGet("mining/clusters/documents")]
        public async Task<IActionResult> GetDocumentClusters()
        {
            var clusters = await _dataMiningService.ClusterDocumentsByRiskAsync();
            return Ok(clusters);
        }

        // ========== Data Mining - Anomaly Detection ==========

        /// <summary>
        /// كشف الأنشطة غير الطبيعية في النظام
        /// </summary>
        [HttpGet("mining/anomalies")]
        public async Task<IActionResult> GetAnomalies([FromQuery] DateTime? fromDate = null)
        {
            var anomalies = await _dataMiningService.DetectAnomalousActivitiesAsync(fromDate);
            return Ok(new { count = anomalies.Count, anomalies });
        }

        /// <summary>
        /// كشف الأنماط الاحتيالية المحتملة
        /// </summary>
        [HttpGet("mining/fraudulent-patterns")]
        public async Task<IActionResult> GetFraudulentPatterns()
        {
            var patterns = await _dataMiningService.DetectFraudulentPatternsAsync();
            return Ok(patterns);
        }

        // ========== Data Mining - Sequence Mining ==========

        /// <summary>
        /// اكتشاف أنماط رحلة المستخدم المتكررة
        /// </summary>
        [HttpGet("mining/patterns/user-journey")]
        public async Task<IActionResult> GetUserJourneyPatterns()
        {
            var patterns = await _dataMiningService.FindUserJourneyPatternsAsync();
            return Ok(patterns);
        }

        /// <summary>
        /// اكتشاف أنماط تطور القضايا
        /// </summary>
        [HttpGet("mining/patterns/case-progression")]
        public async Task<IActionResult> GetCaseProgressionPatterns()
        {
            var patterns = await _dataMiningService.FindCaseProgressionPatternsAsync();
            return Ok(patterns);
        }

        // ========== Classification ==========

        /// <summary>
        /// تصنيف مخاطر القضية (High/Medium/Low)
        /// </summary>
        [HttpGet("classify/case/{caseId}")]
        public async Task<IActionResult> ClassifyCaseRisk(Guid caseId)
        {
            var result = await _dataMiningService.ClassifyCaseRiskAsync(caseId);
            return Ok(result);
        }

        /// <summary>
        /// تصنيف نوع المستند تلقائياً
        /// </summary>
        [HttpGet("classify/document/{documentId}")]
        public async Task<IActionResult> ClassifyDocumentType(Guid documentId)
        {
            var result = await _dataMiningService.ClassifyDocumentTypeAsync(documentId);
            return Ok(result);
        }

        /// <summary>
        /// توقع نتيجة القضية (نجاح/فشل)
        /// </summary>
        [HttpGet("classify/predict-outcome/{caseId}")]
        public async Task<IActionResult> PredictCaseOutcome(Guid caseId)
        {
            var result = await _dataMiningService.PredictCaseOutcomeAsync(caseId);
            return Ok(result);
        }
    }
}