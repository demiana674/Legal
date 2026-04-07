// LegalMateAI.BLL/Services/Service/RiskMapper.cs
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.Service
{
    public static class RiskMapper
    {
        public static RiskLevel MapToRiskLevel(string level)
        {
            if (string.IsNullOrWhiteSpace(level))
                return RiskLevel.Medium;

            level = level.ToLower().Trim();

            if (level.Contains("critical") || level.Contains("حرج"))
                return RiskLevel.Critical;

            if (level.Contains("high") || level.Contains("عالي"))
                return RiskLevel.High;

            if (level.Contains("medium") || level.Contains("متوسط"))
                return RiskLevel.Medium;

            if (level.Contains("low") || level.Contains("منخفض"))
                return RiskLevel.Low;

            return RiskLevel.Medium;
        }

        public static string MapToString(RiskLevel level)
        {
            return level switch
            {
                RiskLevel.Critical => "Critical",
                RiskLevel.High => "High",
                RiskLevel.Medium => "Medium",
                RiskLevel.Low => "Low",
                _ => "Medium"
            };
        }
    }
}