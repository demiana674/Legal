// LegalMateAI.BLL/ML/RecommendationModel.cs
namespace LegalMateAI.BLL.ML
{
    /// <summary>
    /// بيانات الإدخال للنموذج
    /// </summary>
    public class RecommendationInput
    {
        public int LawyerId { get; set; }
        public int SpecializationId { get; set; }
        public int GovernorateId { get; set; }
        public int YearsOfExperience { get; set; }
        public double AverageRating { get; set; }
        public int TotalCases { get; set; }
        public int SuccessRate { get; set; }  // نسبة النجاح في القضايا
    }

    /// <summary>
    /// بيانات التقييمات للـ Collaborative Filtering
    /// </summary>
    public class RatingData
    {
        public int UserId { get; set; }
        public int LawyerId { get; set; }
        public int Rating { get; set; }  // 1-5
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// نتيجة التوصية
    /// </summary>
    public class RecommendationResult
    {
        public int LawyerId { get; set; }
        public double Score { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}