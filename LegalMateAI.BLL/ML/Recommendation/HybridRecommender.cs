namespace LegalMateAI.BLL.ML.Recommendation
{
    /// <summary>
    /// نظام توصية هجين (Content-Based + Collaborative Filtering)
    /// </summary>
    public class HybridRecommender
    {
        private readonly ContentBasedFilter _contentBased;
        private readonly CollaborativeFilter _collaborative;

        public HybridRecommender()
        {
            _contentBased = new ContentBasedFilter();
            _collaborative = new CollaborativeFilter();
        }

        /// <summary>
        /// توليد توصيات هجينة بدمج الطريقتين
        /// </summary>
        public async Task<List<RecommendationResult>> GetHybridRecommendationsAsync(
            Guid userId,
            string? caseDescription = null,
            double contentWeight = 0.6,
            double collaborativeWeight = 0.4)
        {
            var contentScores = await _contentBased.GetRecommendationsAsync(userId, caseDescription);
            var collabScores = await _collaborative.GetRecommendationsAsync(userId);
            
            var combined = new Dictionary<Guid, double>();
            
            // دمج النتائج مع الأوزان
            foreach (var item in contentScores)
                combined[item.Key] = item.Value * contentWeight;
            
            foreach (var item in collabScores)
            {
                if (combined.ContainsKey(item.Key))
                    combined[item.Key] += item.Value * collaborativeWeight;
                else
                    combined[item.Key] = item.Value * collaborativeWeight;
            }
            
            return combined
                .OrderByDescending(x => x.Value)
                .Select(x => new RecommendationResult { LawyerId = x.Key, Score = x.Value })
                .ToList();
        }
    }

    public class RecommendationResult
    {
        public Guid LawyerId { get; set; }
        public double Score { get; set; }
    }
}