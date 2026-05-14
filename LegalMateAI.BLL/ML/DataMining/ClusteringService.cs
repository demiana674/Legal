namespace LegalMateAI.BLL.ML.DataMining
{
    /// <summary>
    /// تجميع المستخدمين/القضايا باستخدام K-Means
    /// </summary>
    public class ClusteringService
    {
        /// <summary>
        /// تجميع النقاط إلى K مجموعات
        /// </summary>
        public List<List<double[]>> KMeansCluster(List<double[]> points, int k, int maxIterations = 100)
        {
            if (points.Count == 0) return new List<List<double[]>>();
            
            var random = new Random();
            var centroids = points.OrderBy(x => random.Next()).Take(k).Select(p => p.ToArray()).ToList();
            var clusters = new List<List<double[]>>();
            bool changed;
            
            for (int iter = 0; iter < maxIterations; iter++)
            {
                clusters = Enumerable.Range(0, k).Select(_ => new List<double[]>()).ToList();
                
                // Assign points to nearest centroid
                foreach (var point in points)
                {
                    var distances = centroids.Select(c => EuclideanDistance(point, c)).ToList();
                    var bestIdx = distances.IndexOf(distances.Min());
                    clusters[bestIdx].Add(point);
                }
                
                // Update centroids
                changed = false;
                for (int i = 0; i < k; i++)
                {
                    if (clusters[i].Count == 0) continue;
                    
                    var newCentroid = new double[points[0].Length];
                    for (int j = 0; j < newCentroid.Length; j++)
                        newCentroid[j] = clusters[i].Average(p => p[j]);
                    
                    if (!centroids[i].SequenceEqual(newCentroid))
                    {
                        centroids[i] = newCentroid;
                        changed = true;
                    }
                }
                
                if (!changed) break;
            }
            
            return clusters;
        }

        /// <summary>
        /// حساب Silhouette Score لتقييم جودة التجميع
        /// </summary>
        public double SilhouetteScore(List<List<double[]>> clusters, List<double[]> allPoints)
        {
            double totalScore = 0;
            
            foreach (var cluster in clusters)
            {
                foreach (var point in cluster)
                {
                    // a = متوسط المسافة لنقاط في نفس المجموعة
                    var a = cluster.Count > 1 
                        ? cluster.Where(p => p != point).Average(p => EuclideanDistance(point, p))
                        : 0;
                    
                    // b = أقل متوسط مسافة لمجموعة أخرى
                    var b = clusters.Where(c => c != cluster)
                        .Select(c => c.Average(p => EuclideanDistance(point, p)))
                        .DefaultIfEmpty(0)
                        .Min();
                    
                    totalScore += (b - a) / Math.Max(a, b);
                }
            }
            
            return allPoints.Count > 0 ? totalScore / allPoints.Count : 0;
        }

        private double EuclideanDistance(double[] a, double[] b)
        {
            if (a.Length != b.Length) return double.MaxValue;
            return Math.Sqrt(a.Zip(b, (ai, bi) => Math.Pow(ai - bi, 2)).Sum());
        }
    }
}