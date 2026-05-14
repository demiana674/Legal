using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LegalMateAI.BLL.ML.DeepLearning
{
    public class LawyerEmbedding
    {
        private Dictionary<Guid, float[]> _embeddings = new();
        private readonly string _embeddingsPath;

        public LawyerEmbedding()
        {
            _embeddingsPath = Path.Combine(Directory.GetCurrentDirectory(), "ML_Models", "lawyer_embeddings.json");
        }

        public async Task GenerateEmbeddingsAsync(List<LawyerData> lawyers)
        {
            _embeddings.Clear();
            
            foreach (var lawyer in lawyers)
            {
                var vector = new float[50];
                
                var specialtyHash = Math.Abs(lawyer.SpecialtyId.GetHashCode()) % 20;
                vector[specialtyHash] = 1;
                
                vector[20 + Math.Min(lawyer.YearsOfExperience, 20)] = 1;
                vector[40 + (int)(lawyer.Rating * 2)] = 1;
                
                _embeddings[lawyer.LawyerId] = vector;
            }
            
            await SaveEmbeddingsAsync();
        }

        public double CalculateSimilarity(Guid lawyerId1, Guid lawyerId2)
        {
            if (!_embeddings.ContainsKey(lawyerId1) || !_embeddings.ContainsKey(lawyerId2))
                return 0;

            var v1 = _embeddings[lawyerId1];
            var v2 = _embeddings[lawyerId2];
            
            double dotProduct = 0, norm1 = 0, norm2 = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                dotProduct += v1[i] * v2[i];
                norm1 += v1[i] * v1[i];
                norm2 += v2[i] * v2[i];
            }
            
            if (norm1 == 0 || norm2 == 0) return 0;
            return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }

        public List<Guid> FindSimilarLawyers(Guid lawyerId, int topK = 5)
        {
            if (!_embeddings.ContainsKey(lawyerId)) return new List<Guid>();
            
            return _embeddings
                .Where(kv => kv.Key != lawyerId)
                .Select(kv => new { kv.Key, Similarity = CalculateSimilarity(lawyerId, kv.Key) })
                .OrderByDescending(x => x.Similarity)
                .Take(topK)
                .Select(x => x.Key)
                .ToList();
        }

        private async Task SaveEmbeddingsAsync()
        {
            var directory = Path.GetDirectoryName(_embeddingsPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);
                
            var json = JsonSerializer.Serialize(_embeddings);
            await File.WriteAllTextAsync(_embeddingsPath, json);
        }
    }

    public class LawyerData
    {
        public Guid LawyerId { get; set; }
        public int SpecialtyId { get; set; }
        public int YearsOfExperience { get; set; }
        public double Rating { get; set; }
    }
}