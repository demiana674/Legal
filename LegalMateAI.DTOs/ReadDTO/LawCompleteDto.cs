namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawCompleteDto
    {
        public LawCoreInfoDto CoreInfo { get; set; } = new();
        public LawFileLinksDto FileLinks { get; set; } = new();
        public LawMetricsDto Metrics { get; set; } = new();
        public LawAuditDto Audit { get; set; } = new();
        public LawContentDto Content { get; set; } = new();
    }
}