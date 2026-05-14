using System.Text.RegularExpressions;

namespace LegalMateAI.BLL.ML.DataMining
{
    /// <summary>
    /// اكتشاف الأنماط المتكررة في البيانات
    /// </summary>
    public class PatternDetector
    {
        /// <summary>
        /// اكتشاف أنماط في النصوص القانونية (مثل: "إذا .. فإن ..")
        /// </summary>
        public List<LegalPattern> DetectLegalPatterns(List<string> documents)
        {
            var patterns = new List<LegalPattern>();
            
            // نمط الشرط الجزائي
            var penaltyPattern = new Regex(@"إذا[\s\S]*?تأخر[\s\S]*?دفع[\s\S]*?غرامة", RegexOptions.IgnoreCase);
            
            // نمط الفسخ
            var terminationPattern = new Regex(@"يحق للطرف[\s\S]*?فسخ[\s\S]*?العقد", RegexOptions.IgnoreCase);
            
            foreach (var doc in documents)
            {
                if (penaltyPattern.IsMatch(doc))
                {
                    patterns.Add(new LegalPattern 
                    { 
                        PatternType = "شرط جزائي", 
                        Frequency = documents.Count(d => penaltyPattern.IsMatch(d)),
                        Example = ExtractExample(doc, penaltyPattern)
                    });
                }
                
                if (terminationPattern.IsMatch(doc))
                {
                    patterns.Add(new LegalPattern 
                    { 
                        PatternType = "حق الفسخ", 
                        Frequency = documents.Count(d => terminationPattern.IsMatch(d)),
                        Example = ExtractExample(doc, terminationPattern)
                    });
                }
            }
            
            return patterns.DistinctBy(p => p.PatternType).ToList();
        }

        private string ExtractExample(string text, Regex pattern)
        {
            var match = pattern.Match(text);
            return match.Success ? match.Value[..Math.Min(match.Value.Length, 200)] : "";
        }
    }

    public class LegalPattern
    {
        public string PatternType { get; set; } = string.Empty;
        public int Frequency { get; set; }
        public string Example { get; set; } = string.Empty;
    }
}