using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IRecommendationService
    {
        Task<List<RecommendedLawyerDto>> RecommendLawyersAsync(
            Guid? userId,
            string? documentAnalysisText = null,
            string? caseDescription = null,
            string? caseType = null,
            int? governorateId = null,
            int topK = 5);

        Task<List<RecommendedLawyerDto>> RecommendByUserHistoryAsync(Guid userId, int topK = 5);
        
        Task<bool> TrainRecommendationModelAsync();
        
        // Helper for document analysis
        Task<string?> GetDocumentAnalysisAsync(Guid documentId);
    }

    public class RecommendedLawyerDto
    {
        public Guid LawyerId { get; set; }
        public Guid UserId { get; set; }
        public string LawyerName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string BarAssociation { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public string? GovernorateName { get; set; }
        public string? City { get; set; }
        public double MatchScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }
}