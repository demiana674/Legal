using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IDataMiningService
    {
        // Association Rules
        Task<List<AssociationRuleDto>> FindCasePatternsAsync(double minSupport = 0.01, double minConfidence = 0.5);
        Task<List<AssociationRuleDto>> FindLawyerClientPatternsAsync();
        
        // Clustering
        Task<ClusteringResultDto> ClusterUsersAsync(int k = 5);
        Task<ClusteringResultDto> ClusterCasesAsync(int k = 5);
        Task<ClusteringResultDto> ClusterDocumentsByRiskAsync();
        
        // Classification
        Task<ClassificationResultDto> ClassifyCaseRiskAsync(Guid caseId);
        Task<ClassificationResultDto> ClassifyDocumentTypeAsync(Guid documentId);
        Task<ClassificationResultDto> PredictCaseOutcomeAsync(Guid caseId);
        
        // Anomaly Detection
        Task<List<AnomalyDto>> DetectAnomalousActivitiesAsync(DateTime? fromDate = null);
        Task<List<AnomalyDto>> DetectFraudulentPatternsAsync();
        
        // Sequence Mining
        Task<List<SequencePatternDto>> FindUserJourneyPatternsAsync();
        Task<List<SequencePatternDto>> FindCaseProgressionPatternsAsync();
    }
}