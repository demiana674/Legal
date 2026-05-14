using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.Service
{
    public class DataMiningService : IDataMiningService
    {
        private readonly LegalMateDbContext _context;
        private readonly ILogger<DataMiningService> _logger;

        public DataMiningService(
            LegalMateDbContext context,
            ILogger<DataMiningService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== Association Rules ==========

        public async Task<List<AssociationRuleDto>> FindCasePatternsAsync(
            double minSupport = 0.01,
            double minConfidence = 0.5)
        {
            _logger.LogInformation($"Finding association rules with support={minSupport}, confidence={minConfidence}");
            
            var rules = new List<AssociationRuleDto>();
            
            var cases = await _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Take(1000)
                .ToListAsync();
            
            if (!cases.Any())
                return rules;
            
            var transactions = new List<List<string>>();
            foreach (var c in cases)
            {
                var items = new List<string>();
                
                if (!string.IsNullOrEmpty(c.CaseType))
                    items.Add($"نوع:{c.CaseType}");
                
                items.Add($"أولوية:{c.Priority}");
                items.Add($"حالة:{c.Status}");
                items.Add(c.LawyerId.HasValue ? "مع:محامي" : "بدون:محامي");
                
                var duration = (DateTime.UtcNow - c.CreatedAt).TotalDays;
                items.Add(duration < 30 ? "مدة:قصيرة" : duration < 90 ? "مدة:متوسطة" : "مدة:طويلة");
                
                transactions.Add(items);
            }
            
            var minCount = (int)(transactions.Count * minSupport);
            var frequentItemsets = FindFrequentItemsets(transactions, minCount);
            
            foreach (var itemset in frequentItemsets.Where(i => i.Count >= 2))
            {
                for (int i = 0; i < itemset.Count; i++)
                {
                    var antecedent = itemset[i];
                    var consequent = itemset.Where((_, idx) => idx != i).ToList();
                    
                    var support = CalculateSupport(transactions, itemset);
                    var confidence = CalculateConfidence(transactions, antecedent, consequent);
                    var lift = CalculateLift(transactions, antecedent, consequent);
                    
                    if (confidence >= minConfidence && lift > 1)
                    {
                        rules.Add(new AssociationRuleDto
                        {
                            Antecedent = antecedent,
                            Consequent = string.Join(" و ", consequent),
                            Support = Math.Round(support, 3),
                            Confidence = Math.Round(confidence, 3),
                            Lift = Math.Round(lift, 3),
                            Interpretation = GenerateRuleInterpretation(antecedent, consequent, confidence)
                        });
                    }
                }
            }
            
            return rules.OrderByDescending(r => r.Lift).Take(20).ToList();
        }

        public async Task<List<AssociationRuleDto>> FindLawyerClientPatternsAsync()
        {
            var rules = new List<AssociationRuleDto>();
            
            var clientLawyerPairs = await _context.Cases
                .Where(c => c.ClientId != Guid.Empty && c.LawyerId != null)
                .GroupBy(c => new { c.ClientId, c.LawyerId })
                .Select(g => new { g.Key.ClientId, g.Key.LawyerId, Count = g.Count() })
                .ToListAsync();
            
            if (!clientLawyerPairs.Any())
                return rules;
            
            var clientTransactions = clientLawyerPairs
                .GroupBy(p => p.ClientId)
                .Select(g => g.Select(p => $"محامي_{p.LawyerId}").ToList())
                .ToList();
            
            var frequentPairs = FindFrequentItemsets(clientTransactions, 2);
            
            foreach (var pair in frequentPairs.Where(p => p.Count == 2))
            {
                rules.Add(new AssociationRuleDto
                {
                    Antecedent = pair[0],
                    Consequent = pair[1],
                    Support = 0.05,
                    Confidence = 0.7,
                    Lift = 1.5,
                    Interpretation = $"العملاء الذين تعاملوا مع {pair[0]}، تعاملوا أيضاً مع {pair[1]}"
                });
            }
            
            return rules;
        }

        // ========== Clustering ==========

        public async Task<ClusteringResultDto> ClusterUsersAsync(int k = 5)
        {
            _logger.LogInformation($"Clustering users into {k} clusters");
            
            var users = await _context.Users
                .Take(500)
                .ToListAsync();
            
            if (!users.Any())
                return new ClusteringResultDto();
            
            // ✅ تم التصحيح: استخدام _context.Cases.Count بدلاً من u.Cases
            var userVectors = users.Select(u => new double[]
            {
                _context.Cases.Count(c => c.ClientId == u.UserID),     // عدد القضايا
                _context.Contracts.Count(c => c.UserId == u.UserID),   // عدد العقود
                (u.LastLogin.HasValue ? 1 : 0),                        // نشاط
                (DateTime.UtcNow - u.CreatedAt).TotalDays / 30,        // عمر الحساب
                u.EmailVerified ? 1 : 0                                // توثيق
            }).ToList();
            
            var clusters = KMeansCluster(userVectors, k);
            var silhouetteScore = CalculateSilhouetteScore(clusters, userVectors);
            
            return new ClusteringResultDto
            {
                NumberOfClusters = k,
                SilhouetteScore = Math.Round(silhouetteScore, 3),
                Clusters = clusters.Select((c, idx) => new ClusterDto
                {
                    Id = idx,
                    Size = c.Count,
                    Characteristics = GetClusterCharacteristics(c, users, idx),
                    Centroids = GetClusterCentroid(c)
                }).ToList()
            };
        }

        public async Task<ClusteringResultDto> ClusterCasesAsync(int k = 5)
        {
            var cases = await _context.Cases
                .Include(c => c.Client)
                .Take(500)
                .ToListAsync();
            
            if (!cases.Any())
                return new ClusteringResultDto();
            
            var caseVectors = cases.Select(c => new double[]
            {
                GetCasePriorityNumeric(c.Priority),
                c.Status == CaseStatus.Completed ? 1 : 0,
                c.LawyerId.HasValue ? 1 : 0,
                (DateTime.UtcNow - c.CreatedAt).TotalDays / 30,
                !string.IsNullOrEmpty(c.CaseType) ? 1 : 0
            }).ToList();
            
            var clusters = KMeansCluster(caseVectors, k);
            var silhouetteScore = CalculateSilhouetteScore(clusters, caseVectors);
            
            return new ClusteringResultDto
            {
                NumberOfClusters = k,
                SilhouetteScore = Math.Round(silhouetteScore, 3),
                Clusters = clusters.Select((c, idx) => new ClusterDto
                {
                    Id = idx,
                    Size = c.Count,
                    Characteristics = GetCaseClusterCharacteristics(c, idx)
                }).ToList()
            };
        }

        public async Task<ClusteringResultDto> ClusterDocumentsByRiskAsync()
        {
            var documents = await _context.Documents
                .Include(d => d.Analyses)
                .Take(500)
                .ToListAsync();
            
            if (!documents.Any())
                return new ClusteringResultDto();
            
            var docVectors = documents.Select(d => new double[]
            {
                d.FileSize / 1024.0,
                d.Status == DocumentStatus.Verified ? 1 : 0,
                d.Analyses.Any(a => a.Status == AnalysisStatus.Completed) ? 1 : 0,
                (int)d.DocType % 10
            }).ToList();
            
            var clusters = KMeansCluster(docVectors, 3);
            var silhouetteScore = CalculateSilhouetteScore(clusters, docVectors);
            
            return new ClusteringResultDto
            {
                NumberOfClusters = 3,
                SilhouetteScore = Math.Round(silhouetteScore, 3),
                Clusters = clusters.Select((c, idx) => new ClusterDto
                {
                    Id = idx,
                    Size = c.Count,
                    Characteristics = new List<string>
                    {
                        idx == 0 ? "مستندات منخفضة الخطورة" :
                        idx == 1 ? "مستندات متوسطة الخطورة" :
                        "مستندات عالية الخطورة"
                    }
                }).ToList()
            };
        }

        // ========== Classification ==========

        public async Task<ClassificationResultDto> ClassifyCaseRiskAsync(Guid caseId)
        {
            var caseEntity = await _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .FirstOrDefaultAsync(c => c.Id == caseId);
            
            if (caseEntity == null)
                return new ClassificationResultDto { PredictedClass = "غير معروف", Confidence = 0 };
            
            double riskScore = 0;
            var reasons = new List<string>();
            
            var priorityScore = caseEntity.Priority switch
            {
                CasePriority.Urgent => 40,
                CasePriority.High => 30,
                CasePriority.Medium => 20,
                _ => 10
            };
            riskScore += priorityScore;
            reasons.Add($"الأولوية: {priorityScore}/40");
            
            var ageDays = (DateTime.UtcNow - caseEntity.CreatedAt).TotalDays;
            var ageScore = ageDays > 180 ? 30 : ageDays > 90 ? 20 : ageDays > 30 ? 10 : 5;
            riskScore += ageScore;
            reasons.Add($"مدة القضية: {ageScore}/30");
            
            var lawyerScore = caseEntity.LawyerId == null ? 20 : 0;
            riskScore += lawyerScore;
            reasons.Add(lawyerScore > 0 ? "بدون محامي: +20" : "مع محامي: 0");
            
            var typeScore = caseEntity.CaseType?.ToLower() switch
            {
                string t when t.Contains("جنائي") => 10,
                string t when t.Contains("مدني") => 5,
                _ => 0
            };
            riskScore += typeScore;
            
            string riskLevel;
            double confidence;
            string explanation;
            
            if (riskScore >= 70)
            {
                riskLevel = "عالي الخطورة";
                confidence = 0.85;
                explanation = $"القضية {riskLevel} بسبب: {string.Join("، ", reasons)}";
            }
            else if (riskScore >= 40)
            {
                riskLevel = "متوسط الخطورة";
                confidence = 0.75;
                explanation = $"القضية {riskLevel}. {string.Join("، ", reasons)}";
            }
            else
            {
                riskLevel = "منخفض الخطورة";
                confidence = 0.9;
                explanation = $"القضية {riskLevel}. {string.Join("، ", reasons)}";
            }
            
            return new ClassificationResultDto
            {
                PredictedClass = riskLevel,
                Confidence = confidence,
                Probabilities = new Dictionary<string, double>
                {
                    { "منخفض", riskScore < 40 ? 0.8 : 0.1 },
                    { "متوسط", riskScore >= 40 && riskScore < 70 ? 0.7 : 0.15 },
                    { "عالي", riskScore >= 70 ? 0.8 : 0.1 }
                },
                TopFeatures = new List<string> { "الأولوية", "مدة القضية", "وجود محامي", "نوع القضية" },
                Explanation = explanation
            };
        }

        public async Task<ClassificationResultDto> ClassifyDocumentTypeAsync(Guid documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            
            if (document == null)
                return new ClassificationResultDto { PredictedClass = "غير معروف", Confidence = 0 };
            
            var extension = Path.GetExtension(document.FileName).ToLower();
            var fileName = document.FileName.ToLower();
            
            string docType;
            double confidence;
            string explanation;
            
            if (extension == ".pdf")
            {
                if (fileName.Contains("عقد") || fileName.Contains("contract"))
                {
                    docType = "عقد قانوني";
                    confidence = 0.85;
                    explanation = "تم تصنيف المستند كـ 'عقد قانوني' بناءً على اسم الملف ونوعه PDF";
                }
                else if (fileName.Contains("قضية") || fileName.Contains("case"))
                {
                    docType = "مستند قضائي";
                    confidence = 0.8;
                    explanation = "تم تصنيف المستند كـ 'مستند قضائي' بناءً على الكلمات المفتاحية في الاسم";
                }
                else
                {
                    docType = "مستند PDF عام";
                    confidence = 0.6;
                    explanation = "تم تصنيف المستند كـ 'مستند PDF عام' (لم يتم التعرف على نوع محدد)";
                }
            }
            else if (extension == ".docx" || extension == ".doc")
            {
                docType = "مستند Word";
                confidence = 0.9;
                explanation = "تم تصنيف المستند كـ 'مستند Word' بناءً على امتداد الملف";
            }
            else if (extension == ".txt")
            {
                docType = "ملف نصي";
                confidence = 0.85;
                explanation = "تم تصنيف المستند كـ 'ملف نصي'";
            }
            else
            {
                docType = "مستند غير معروف";
                confidence = 0.5;
                explanation = "لم يتم التعرف على نوع المستند بدقة";
            }
            
            return new ClassificationResultDto
            {
                PredictedClass = docType,
                Confidence = confidence,
                Probabilities = new Dictionary<string, double>
                {
                    { "عقد", docType == "عقد قانوني" ? confidence : 0.1 },
                    { "قضائي", docType == "مستند قضائي" ? confidence : 0.1 },
                    { "آخر", docType == "مستند PDF عام" ? confidence : 0.2 }
                },
                TopFeatures = new List<string> { "امتداد الملف", "اسم الملف", "الكلمات المفتاحية" },
                Explanation = explanation
            };
        }

        public async Task<ClassificationResultDto> PredictCaseOutcomeAsync(Guid caseId)
        {
            var caseEntity = await _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .FirstOrDefaultAsync(c => c.Id == caseId);
            
            if (caseEntity == null)
                return new ClassificationResultDto { PredictedClass = "غير معروف", Confidence = 0 };
            
            double successProbability = 0.5;
            
            if (caseEntity.LawyerId != null)
                successProbability += 0.25;
            
            if (caseEntity.Lawyer != null)
            {
                var experience = caseEntity.Lawyer.YearsOfExperience ?? 0;
                if (experience >= 10)
                    successProbability += 0.15;
                else if (experience >= 5)
                    successProbability += 0.08;
                else if (experience >= 2)
                    successProbability += 0.03;
            }
            
            successProbability += caseEntity.Priority switch
            {
                CasePriority.Urgent => 0.1,
                CasePriority.High => 0.05,
                CasePriority.Medium => 0,
                _ => -0.05
            };
            
            var hasDocuments = await _context.CaseDocuments.AnyAsync(d => d.CaseId == caseId);
            if (hasDocuments)
                successProbability += 0.1;
            
            successProbability = Math.Min(successProbability, 0.95);
            successProbability = Math.Max(successProbability, 0.1);
            
            var outcome = successProbability >= 0.6 ? "متوقع النجاح" : "متوقع عدم النجاح";
            
            return new ClassificationResultDto
            {
                PredictedClass = outcome,
                Confidence = Math.Round(successProbability, 2),
                Probabilities = new Dictionary<string, double>
                {
                    { "نجاح", successProbability },
                    { "عدم نجاح", 1 - successProbability }
                },
                TopFeatures = new List<string> { "وجود محامي", "خبرة المحامي", "أولوية القضية", "المستندات" },
                Explanation = $"احتمالية نجاح القضية: {successProbability:P0}. " +
                            $"{(caseEntity.LawyerId != null ? "يوجد محامي متخصص. " : "لا يوجد محامي. ")}" +
                            $"خبرة المحامي: {caseEntity.Lawyer?.YearsOfExperience ?? 0} سنة"
            };
        }

        // ========== Anomaly Detection ==========

        public async Task<List<AnomalyDto>> DetectAnomalousActivitiesAsync(DateTime? fromDate = null)
        {
            var anomalies = new List<AnomalyDto>();
            var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
            
            var userActivity = await _context.AdminLogs
                .Where(l => l.Timestamp >= startDate)
                .GroupBy(l => l.ActorId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();
            
            if (userActivity.Any())
            {
                var avgActivity = userActivity.Average(u => u.Count);
                var stdDev = Math.Sqrt(userActivity.Average(u => Math.Pow(u.Count - avgActivity, 2)));
                
                foreach (var user in userActivity.Where(u => u.Count > avgActivity + 2 * stdDev))
                {
                    anomalies.Add(new AnomalyDto
                    {
                        Id = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow,
                        EntityType = "User",
                        EntityId = user.UserId.ToString(),
                        AnomalyType = "معدل نشاط غير طبيعي",
                        AnomalyScore = Math.Round((user.Count - avgActivity) / stdDev, 2),
                        Description = $"المستخدم قام بـ {user.Count} نشاط، بينما المتوسط {avgActivity:F0}",
                        IsConfirmed = false
                    });
                }
            }
            
            var suspiciousReviews = await _context.LawyerReviews
                .Where(r => r.CreatedAt >= startDate)
                .GroupBy(r => r.LawyerId)
                .Select(g => new
                {
                    LawyerId = g.Key,
                    AvgRating = g.Average(r => r.Rating),
                    RecentRatings = g.Count(r => r.CreatedAt > DateTime.UtcNow.AddDays(-7))
                })
                .Where(x => x.RecentRatings > 3 && x.AvgRating > 4.8)
                .ToListAsync();
            
            foreach (var review in suspiciousReviews)
            {
                anomalies.Add(new AnomalyDto
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    EntityType = "Lawyer",
                    EntityId = review.LawyerId.ToString(),
                    AnomalyType = "تقييمات غير معتادة",
                    AnomalyScore = review.AvgRating,
                    Description = $"تقييمات مرتفعة بشكل غير طبيعي: {review.AvgRating:F1} من 5 (آخر {review.RecentRatings} تقييم)",
                    IsConfirmed = false
                });
            }
            
            return anomalies;
        }

        public async Task<List<AnomalyDto>> DetectFraudulentPatternsAsync()
        {
            var anomalies = new List<AnomalyDto>();
            
            var failedLogins = await _context.LoginAttempts
                .Where(l => !l.IsSuccess && l.AttemptedAt > DateTime.UtcNow.AddHours(-1))
                .GroupBy(l => l.Email)
                .Select(g => new { Email = g.Key, FailedCount = g.Count() })
                .Where(x => x.FailedCount > 5)
                .ToListAsync();
            
            foreach (var attempt in failedLogins)
            {
                anomalies.Add(new AnomalyDto
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    EntityType = "Login",
                    EntityId = attempt.Email,
                    AnomalyType = "محاولات دخول فاشلة",
                    AnomalyScore = attempt.FailedCount,
                    Description = $"{attempt.FailedCount} محاولة دخول فاشلة للبريد {attempt.Email} في آخر ساعة",
                    IsConfirmed = false
                });
            }
            
            return anomalies;
        }

        // ========== Sequence Mining ==========

        public async Task<List<SequencePatternDto>> FindUserJourneyPatternsAsync()
        {
            var patterns = new List<SequencePatternDto>();
            
            var userActions = await _context.AdminLogs
                .OrderBy(l => l.ActorId)
                .ThenBy(l => l.Timestamp)
                .Take(2000)
                .ToListAsync();
            
            if (!userActions.Any())
                return patterns;
            
            var userSequences = userActions
                .GroupBy(l => l.ActorId)
                .Select(g => g.Select(l => l.Action.ToString()).ToList())
                .Where(s => s.Count >= 2)
                .ToList();
            
            var frequentPatterns = FindFrequentSequences(userSequences, 3);
            
            foreach (var pattern in frequentPatterns.Take(10))
            {
                patterns.Add(new SequencePatternDto
                {
                    Pattern = string.Join(" ➜ ", pattern),
                    Support = userSequences.Count(s => ContainsSequence(s, pattern)),
                    Confidence = 0.75,
                    AverageDuration = 5,
                    Recommendation = GetRecommendationForPattern(pattern)
                });
            }
            
            return patterns;
        }

        public async Task<List<SequencePatternDto>> FindCaseProgressionPatternsAsync()
        {
            var patterns = new List<SequencePatternDto>();
            
            var caseStats = await _context.Cases
                .Select(c => new
                {
                    c.Status,
                    c.Priority,
                    HasDoc = _context.CaseDocuments.Any(d => d.CaseId == c.Id),
                    HasNote = _context.CaseNotes.Any(n => n.CaseId == c.Id),
                    HasLawyer = c.LawyerId != null
                })
                .ToListAsync();
            
            if (caseStats.Any())
            {
                var completedCount = caseStats.Count(c => c.Status == CaseStatus.Completed);
                var rejectedCount = caseStats.Count(c => c.Status == CaseStatus.Rejected);
                var total = caseStats.Count;
                
                patterns.Add(new SequencePatternDto
                {
                    Pattern = "إنشاء قضية ➜ تعيين محامي ➜ رفع مستندات ➜ إكمال",
                    Support = completedCount,
                    Confidence = total > 0 ? Math.Round((double)completedCount / total * 100, 1) : 0,
                    Recommendation = "لزيادة فرص النجاح، يُنصح بتعيين محامي متخصص مبكراً وتقديم المستندات كاملة"
                });
                
                patterns.Add(new SequencePatternDto
                {
                    Pattern = "إنشاء قضية ➜ تأخير > 30 يوم ➜ إلغاء",
                    Support = rejectedCount,
                    Confidence = total > 0 ? Math.Round((double)rejectedCount / total * 100, 1) : 0,
                    Recommendation = "يجب متابعة القضايا المعلقة أسبوعياً ومنع التأخير الطويل"
                });
                
                var hasLawyerCount = caseStats.Count(c => c.HasLawyer);
                patterns.Add(new SequencePatternDto
                {
                    Pattern = "مع محامي ➜ نسبة نجاح أعلى",
                    Support = hasLawyerCount,
                    Confidence = hasLawyerCount > 0 ? Math.Round((double)completedCount / hasLawyerCount * 100, 1) : 0,
                    Recommendation = "تأكد من أن كل قضية لديها محامي معين لزيادة فرص النجاح"
                });
            }
            
            return patterns;
        }

        // ========== Private Helper Methods ==========

        private List<List<string>> FindFrequentItemsets(List<List<string>> transactions, int minCount)
        {
            var frequentItemsets = new List<List<string>>();
            
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
            
            var frequentSingles = itemCounts
                .Where(kv => kv.Value >= minCount)
                .Select(kv => kv.Key)
                .ToList();
            
            frequentItemsets.AddRange(frequentSingles.Select(s => new List<string> { s }));
            
            for (int i = 0; i < frequentSingles.Count; i++)
            {
                for (int j = i + 1; j < frequentSingles.Count; j++)
                {
                    var pair = new List<string> { frequentSingles[i], frequentSingles[j] };
                    var pairCount = transactions.Count(t => pair.All(p => t.Contains(p)));
                    
                    if (pairCount >= minCount)
                        frequentItemsets.Add(pair);
                }
            }
            
            return frequentItemsets;
        }

        private double CalculateSupport(List<List<string>> transactions, List<string> itemset)
        {
            if (!transactions.Any()) return 0;
            var count = transactions.Count(t => itemset.All(i => t.Contains(i)));
            return (double)count / transactions.Count;
        }

        private double CalculateConfidence(List<List<string>> transactions, string antecedent, List<string> consequent)
        {
            var antecedentCount = transactions.Count(t => t.Contains(antecedent));
            if (antecedentCount == 0) return 0;
            var bothCount = transactions.Count(t => t.Contains(antecedent) && consequent.All(c => t.Contains(c)));
            return (double)bothCount / antecedentCount;
        }

        private double CalculateLift(List<List<string>> transactions, string antecedent, List<string> consequent)
        {
            var confidence = CalculateConfidence(transactions, antecedent, consequent);
            var consequentCount = (double)transactions.Count(t => consequent.All(c => t.Contains(c))) / transactions.Count;
            return consequentCount == 0 ? 0 : confidence / consequentCount;
        }

        private string GenerateRuleInterpretation(string antecedent, List<string> consequent, double confidence)
        {
            var antecedentClean = antecedent.Split(':').Last();
            var consequentClean = string.Join(" و ", consequent.Select(c => c.Split(':').Last()));
            
            if (confidence >= 0.8)
                return $"غالباً (بنسبة {confidence:P0})، عندما يكون {antecedentClean}، يحدث {consequentClean}";
            else if (confidence >= 0.6)
                return $"في {confidence:P0} من الحالات، يرتبط {antecedentClean} بـ {consequentClean}";
            else
                return $"هناك ارتباط ضعيف بين {antecedentClean} و {consequentClean}";
        }

        private List<List<double[]>> KMeansCluster(List<double[]> points, int k, int maxIterations = 100)
        {
            if (points.Count == 0) return new List<List<double[]>>();
            
            var random = new Random();
            var centroids = points.OrderBy(x => random.Next()).Take(k).Select(p => p.ToArray()).ToList();
            var clusters = new List<List<double[]>>();
            bool changed;
            
            for (int iter = 0; iter < maxIterations; iter++)
            {
                clusters = Enumerable.Range(0, k).Select(_ => new List<double[]>()).ToList();
                
                foreach (var point in points)
                {
                    var distances = centroids.Select(c => EuclideanDistance(point, c)).ToList();
                    var bestIdx = distances.IndexOf(distances.Min());
                    clusters[bestIdx].Add(point);
                }
                
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

        private double EuclideanDistance(double[] a, double[] b)
        {
            if (a.Length != b.Length) return double.MaxValue;
            return Math.Sqrt(a.Zip(b, (ai, bi) => Math.Pow(ai - bi, 2)).Sum());
        }

        private double CalculateSilhouetteScore(List<List<double[]>> clusters, List<double[]> allPoints)
        {
            if (allPoints.Count < 2) return 1;
            
            double totalScore = 0;
            
            foreach (var cluster in clusters)
            {
                foreach (var point in cluster)
                {
                    var a = cluster.Count > 1
                        ? cluster.Where(p => p != point).Average(p => EuclideanDistance(point, p))
                        : 0;
                    
                    var b = clusters.Where(c => c != cluster)
                        .Select(c => c.Average(p => EuclideanDistance(point, p)))
                        .DefaultIfEmpty(0)
                        .Min();
                    
                    if (a < b)
                        totalScore += 1 - (a / b);
                    else if (a > b)
                        totalScore += (b / a) - 1;
                    else
                        totalScore += 0;
                }
            }
            
            return allPoints.Count > 0 ? totalScore / allPoints.Count : 0;
        }

        private List<string> GetClusterCharacteristics(List<double[]> cluster, List<User> users, int clusterId)
        {
            var characteristics = new List<string>();
            
            if (!cluster.Any() || !users.Any()) 
                return new List<string> { "بيانات غير كافية" };
            
            var avgCases = cluster.Average(c => c[0]);
            var avgContracts = cluster.Average(c => c[1]);
            var avgActivity = cluster.Average(c => c[2]);
            
            var overallAvgCases = users.Average(u => _context.Cases.Count(c => c.ClientId == u.UserID));
            var overallAvgContracts = users.Average(u => _context.Contracts.Count(c => c.UserId == u.UserID));
            
            if (avgCases > overallAvgCases * 1.5)
                characteristics.Add("نشاط عالي في القضايا");
            else if (avgCases < overallAvgCases * 0.5)
                characteristics.Add("قليل القضايا");
            
            if (avgContracts > overallAvgContracts * 1.5)
                characteristics.Add("نشاط تعاقدي عالي");
            
            if (avgActivity > 0.8)
                characteristics.Add("نشط مؤخراً");
            else if (avgActivity < 0.2)
                characteristics.Add("غير نشط");
            
            if (!characteristics.Any())
                characteristics.Add("مستخدمون عاديون");
            
            return characteristics;
        }

        private Dictionary<string, double> GetClusterCentroid(List<double[]> cluster)
        {
            if (!cluster.Any()) return new Dictionary<string, double>();
            
            return new Dictionary<string, double>
            {
                ["avg_cases"] = Math.Round(cluster.Average(c => c[0]), 2),
                ["avg_contracts"] = Math.Round(cluster.Average(c => c[1]), 2),
                ["avg_activity"] = Math.Round(cluster.Average(c => c[2]), 2),
                ["avg_account_age"] = Math.Round(cluster.Average(c => c[3]), 1)
            };
        }

        private List<string> GetCaseClusterCharacteristics(List<double[]> cluster, int clusterId)
        {
            var characteristics = new List<string>();
            
            if (!cluster.Any()) return new List<string> { "بيانات غير كافية" };
            
            var avgDuration = cluster.Average(c => c[3]);
            var completedRate = cluster.Count(c => c[1] == 1) / (double)cluster.Count;
            
            if (avgDuration > 6)
                characteristics.Add("قضايا قديمة (> 6 أشهر)");
            else if (avgDuration > 3)
                characteristics.Add("قضايا متوسطة العمر");
            else
                characteristics.Add("قضايا جديدة");
            
            if (completedRate > 0.7)
                characteristics.Add("نسبة إنجاز عالية");
            else if (completedRate < 0.3)
                characteristics.Add("نسبة إنجاز منخفضة");
            
            var withLawyerRate = cluster.Count(c => c[2] == 1) / (double)cluster.Count;
            if (withLawyerRate > 0.8)
                characteristics.Add("معظمها بمحامي");
            else if (withLawyerRate < 0.3)
                characteristics.Add("معظمها بدون محامي");
            
            return characteristics;
        }

        private double GetCasePriorityNumeric(CasePriority priority) => priority switch
        {
            CasePriority.Low => 1,
            CasePriority.Medium => 2,
            CasePriority.High => 3,
            CasePriority.Urgent => 4,
            _ => 2
        };

        private List<List<string>> FindFrequentSequences(List<List<string>> sequences, int minSupport)
        {
            var frequent = new List<List<string>>();
            var allItems = sequences.SelectMany(s => s).Distinct().ToList();
            
            foreach (var item in allItems)
            {
                var count = sequences.Count(s => s.Contains(item));
                if (count >= minSupport)
                    frequent.Add(new List<string> { item });
            }
            
            for (int i = 0; i < Math.Min(5, allItems.Count); i++)
            {
                for (int j = i + 1; j < Math.Min(10, allItems.Count); j++)
                {
                    var pair = new List<string> { allItems[i], allItems[j] };
                    var count = sequences.Count(s => ContainsSequence(s, pair));
                    if (count >= minSupport)
                        frequent.Add(pair);
                }
            }
            
            return frequent;
        }

        private bool ContainsSequence(List<string> sequence, List<string> pattern)
        {
            if (pattern.Count == 0) return true;
            if (pattern.Count > sequence.Count) return false;
            
            for (int i = 0; i <= sequence.Count - pattern.Count; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Count; j++)
                {
                    if (sequence[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
            return false;
        }

        private string GetRecommendationForPattern(List<string> pattern)
        {
            if (pattern.Contains("Login") && pattern.Contains("Upload"))
                return "قم بتمكين الإشعارات الفورية بعد الرفع";
            
            if (pattern.Contains("Search") && pattern.Contains("View"))
                return "أضف خاصية 'الحجز السريع' للمستخدمين المتكررين";
            
            if (pattern.Contains("Create") && pattern.Contains("Update"))
                return "حسّن واجهة التعديل للمستخدمين النشطين";
            
            return "تابع تجربة المستخدم وحسّن المسارات الأكثر استخداماً";
        }
    }
}