using System;
using System.Collections.Generic;
using System.Linq;

namespace LegalMateAI.BLL.ML.DataMining
{
    /// <summary>
    /// اكتشاف قواعد الارتباط (مثال: العملاء الذين استخدموا X، استخدموا أيضاً Y)
    /// </summary>
    public class AssociationRules
    {
        /// <summary>
        /// توليد قواعد الارتباط من المعاملات
        /// </summary>
        public List<AssociationRule> GenerateRules(List<List<string>> transactions, double minSupport = 0.01, double minConfidence = 0.5)
        {
            var rules = new List<AssociationRule>();
            
            if (transactions == null || !transactions.Any())
                return rules;
            
            // حساب تكرار كل عنصر
            var itemCounts = new Dictionary<string, int>();
            foreach (var trans in transactions)
            {
                foreach (var item in trans.Distinct())
                {
                    if (itemCounts.ContainsKey(item))
                        itemCounts[item]++;
                    else
                        itemCounts[item] = 1;
                }
            }
            
            var totalTrans = transactions.Count;
            var frequentItems = itemCounts
                .Where(kv => (double)kv.Value / totalTrans >= minSupport)
                .Select(kv => kv.Key)
                .ToList();
            
            // توليد الأزواج
            for (int i = 0; i < frequentItems.Count; i++)
            {
                for (int j = i + 1; j < frequentItems.Count; j++)
                {
                    var antecedent = frequentItems[i];
                    var consequent = frequentItems[j];
                    
                    var support = (double)transactions.Count(t => t.Contains(antecedent) && t.Contains(consequent)) / totalTrans;
                    
                    // ✅ السطر المصحح
                    var confidence = (double)transactions.Count(t => t.Contains(antecedent) && t.Contains(consequent)) / 
                                     Math.Max(1, transactions.Count(t => t.Contains(antecedent)));
                    
                    var consequentSupport = (double)transactions.Count(t => t.Contains(consequent)) / totalTrans;
                    var lift = consequentSupport > 0 ? confidence / consequentSupport : 0;
                    
                    if (support >= minSupport && confidence >= minConfidence)
                    {
                        rules.Add(new AssociationRule
                        {
                            Antecedent = antecedent,
                            Consequent = consequent,
                            Support = support,
                            Confidence = confidence,
                            Lift = lift
                        });
                    }
                }
            }
            
            return rules.OrderByDescending(r => r.Lift).ToList();
        }
    }

    public class AssociationRule
    {
        public string Antecedent { get; set; } = string.Empty;
        public string Consequent { get; set; } = string.Empty;
        public double Support { get; set; }
        public double Confidence { get; set; }
        public double Lift { get; set; }
    }
}