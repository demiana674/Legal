using System.Collections.Generic;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ClusteringResultDto
    {
        public int NumberOfClusters { get; set; }
        public double SilhouetteScore { get; set; }
        public List<ClusterDto> Clusters { get; set; } = new();
        public Dictionary<string, object> ClusterSummary { get; set; } = new();
    }

    public class ClusterDto
    {
        public int Id { get; set; }
        public int Size { get; set; }
        public List<string> Characteristics { get; set; } = new();
        public Dictionary<string, double> Centroids { get; set; } = new();
        public List<object> Members { get; set; } = new();
    }
}