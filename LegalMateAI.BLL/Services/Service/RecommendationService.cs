using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Infrastructure.Services.IService;
using System.Text.RegularExpressions;

namespace LegalMateAI.BLL.Services.Service
{
    public class RecommendationService : IRecommendationService
    {
        private readonly LegalMateDbContext _context;
        private readonly IAIService _aiService;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            LegalMateDbContext context,
            IAIService aiService,
            IEncryptionService encryption,
            ILogger<RecommendationService> logger)
        {
            _context = context;
            _aiService = aiService;
            _encryption = encryption;
            _logger = logger;
        }

        public async Task<List<RecommendedLawyerDto>> RecommendLawyersAsync(
            Guid? userId,
            string? documentAnalysisText = null,
            string? caseDescription = null,
            string? caseType = null,
            int? governorateId = null,
            int topK = 5)
        {
            var activeLawyers = await _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                    .ThenInclude(ls => ls.Specialty)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Governorate)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.City)
                .Where(u => u.Role == UserRole.Lawyer &&
                            u.IsActive &&
                            u.LawyerProfile != null &&
                            u.LawyerProfile.VerificationStatus == LawyerVerificationStatus.Active)
                .ToListAsync();

            if (!activeLawyers.Any())
                return new List<RecommendedLawyerDto>();

            string detectedSpecialization = await DetectSpecializationFromTextAsync(
                documentAnalysisText ?? caseDescription ?? "");

            var scoredLawyers = new List<(User lawyer, double score, string reason)>();

            foreach (var lawyer in activeLawyers)
            {
                var (score, reason) = CalculateMatchScore(
                    lawyer,
                    detectedSpecialization,
                    caseType,
                    governorateId);

                scoredLawyers.Add((lawyer, score, reason));
            }

            var topLawyers = scoredLawyers
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .Take(topK)
                .ToList();

            var result = new List<RecommendedLawyerDto>();
            foreach (var (lawyer, score, reason) in topLawyers)
            {
                var avgRating = lawyer.LawyerProfile!.Reviews?.Any() == true
                    ? lawyer.LawyerProfile.Reviews.Average(r => r.Rating)
                    : 0;

                result.Add(new RecommendedLawyerDto
                {
                    LawyerId = lawyer.LawyerProfile!.Id,
                    UserId = lawyer.UserID,
                    LawyerName = lawyer.FullName,
                    ProfilePicture = lawyer.ProfilePicture,
                    Specialization = lawyer.LawyerProfile.Specialties?
                        .FirstOrDefault()?.Specialty?.NameAr ?? "قانون عام",
                    BarAssociation = lawyer.LawyerProfile.BarAssociation ?? "",
                    YearsOfExperience = lawyer.LawyerProfile.YearsOfExperience ?? 0,
                    Rating = Math.Round(avgRating, 1),
                    TotalReviews = lawyer.LawyerProfile.Reviews?.Count ?? 0,
                    GovernorateName = lawyer.LawyerProfile.Governorate?.Name,
                    City = lawyer.LawyerProfile.City?.Name,
                    MatchScore = Math.Round(score, 1),
                    MatchReason = reason
                });
            }

            return result;
        }

        public async Task<List<RecommendedLawyerDto>> RecommendByUserHistoryAsync(Guid userId, int topK = 5)
        {
            var userRatings = await _context.LawyerReviews
                .Where(r => r.UserId == userId && r.Rating >= 4)
                .Select(r => r.LawyerId)
                .Distinct()
                .ToListAsync();

            if (!userRatings.Any())
            {
                return await RecommendLawyersAsync(userId, topK: topK);
            }

            var similarLawyers = await _context.LawyerProfileSpecialties
                .Where(lps => userRatings.Contains(lps.LawyerId))
                .Select(lps => lps.SpecialtyId)
                .Distinct()
                .ToListAsync();

            var recommendedLawyers = await _context.Users
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Specialties)
                .Include(u => u.LawyerProfile)
                    .ThenInclude(lp => lp!.Reviews)
                .Where(u => u.Role == UserRole.Lawyer &&
                            u.IsActive &&
                            u.LawyerProfile!.VerificationStatus == LawyerVerificationStatus.Active &&
                            u.LawyerProfile.Specialties.Any(s => similarLawyers.Contains(s.SpecialtyId)) &&
                            !userRatings.Contains(u.LawyerProfile!.Id))
                .Take(topK)
                .ToListAsync();

            var result = new List<RecommendedLawyerDto>();
            foreach (var lawyer in recommendedLawyers)
            {
                var avgRating = lawyer.LawyerProfile!.Reviews?.Any() == true
                    ? lawyer.LawyerProfile.Reviews.Average(r => r.Rating)
                    : 0;

                result.Add(new RecommendedLawyerDto
                {
                    LawyerId = lawyer.LawyerProfile.Id,
                    UserId = lawyer.UserID,
                    LawyerName = lawyer.FullName,
                    ProfilePicture = lawyer.ProfilePicture,
                    Specialization = lawyer.LawyerProfile.Specialties?
                        .FirstOrDefault()?.Specialty?.NameAr ?? "قانون عام",
                    BarAssociation = lawyer.LawyerProfile.BarAssociation ?? "",
                    YearsOfExperience = lawyer.LawyerProfile.YearsOfExperience ?? 0,
                    Rating = Math.Round(avgRating, 1),
                    TotalReviews = lawyer.LawyerProfile.Reviews?.Count ?? 0,
                    GovernorateName = lawyer.LawyerProfile.Governorate?.Name,
                    City = lawyer.LawyerProfile.City?.Name,
                    MatchScore = 85,
                    MatchReason = "موصى به بناءً على تقييماتك السابقة"
                });
            }

            return result;
        }

        public Task<bool> TrainRecommendationModelAsync()
        {
            _logger.LogInformation("Training recommendation model...");
            return Task.FromResult(true);
        }

        public async Task<string?> GetDocumentAnalysisAsync(Guid documentId)
        {
            var analysis = await _context.DocumentAnalyses
                .FirstOrDefaultAsync(a => a.DocumentId == documentId && a.Status == AnalysisStatus.Completed);
            
            return analysis?.Summary ?? analysis?.Result;
        }

        // ========== Private Helper Methods ==========

        private async Task<string> DetectSpecializationFromTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            var keywords = new Dictionary<string, string>
            {
                { "مدني|civil|تعويض|ديون|قرض", "مدني" },
                { "جنائي|criminal|سرقة|قتل|ضرب|احتيال", "جنائي" },
                { "أسرة|family|طلاق|حضانة|نفقة|زواج", "أحوال شخصية" },
                { "عمل|labor|موظف|عمال|فصل|مرتب", "عمالي" },
                { "تجاري|commercial|شركة|شركات|عقود|مقاولات", "تجاري" },
                { "عقاري|real estate|إيجار|بيع|عقار|أرض", "عقاري" },
                { "ضرائب|tax|ضريبة|جباية", "ضريبي" },
                { "إداري|administrative|مجلس الدولة|قرار إداري", "إداري" }
            };

            foreach (var keyword in keywords)
            {
                if (Regex.IsMatch(text, keyword.Key, RegexOptions.IgnoreCase))
                    return keyword.Value;
            }

            return "";
        }

        private (double score, string reason) CalculateMatchScore(
            User lawyer,
            string detectedSpecialization,
            string? caseType,
            int? governorateId)
        {
            double score = 0;
            var reasons = new List<string>();

            if (!string.IsNullOrEmpty(detectedSpecialization))
            {
                var lawyerSpecialties = lawyer.LawyerProfile!.Specialties?
                    .Select(s => s.Specialty?.NameAr ?? "") ?? new List<string>();

                if (lawyerSpecialties.Any(s => s.Contains(detectedSpecialization)))
                {
                    score += 40;
                    reasons.Add($"✅ متخصص في {detectedSpecialization}");
                }
                else
                {
                    score += 10;
                    reasons.Add($"⚠️ ليس متخصصاً في {detectedSpecialization}");
                }
            }

            if (governorateId.HasValue && lawyer.LawyerProfile!.GovernorateId == governorateId.Value)
            {
                score += 20;
                reasons.Add($"📍 في نفس المحافظة");
            }
            else if (governorateId.HasValue)
            {
                score += 5;
            }
            else
            {
                score += 10;
            }

            var experience = lawyer.LawyerProfile!.YearsOfExperience ?? 0;
            if (experience >= 10)
            {
                score += 15;
                reasons.Add($"⭐ خبرة {experience} سنوات");
            }
            else if (experience >= 5)
            {
                score += 10;
                reasons.Add($"⭐ خبرة {experience} سنوات");
            }
            else
            {
                score += 5;
            }

            var avgRating = lawyer.LawyerProfile.Reviews?.Any() == true
                ? lawyer.LawyerProfile.Reviews.Average(r => r.Rating)
                : 0;

            if (avgRating >= 4.5)
            {
                score += 15;
                reasons.Add($"🏆 تقييم {avgRating:0.0}/5");
            }
            else if (avgRating >= 4.0)
            {
                score += 10;
                reasons.Add($"👍 تقييم {avgRating:0.0}/5");
            }
            else if (avgRating >= 3.0)
            {
                score += 5;
            }

            var totalCases = lawyer.LawyerProfile.Reviews?.Count ?? 0;
            if (totalCases >= 20)
            {
                score += 10;
                reasons.Add($"📋 {totalCases} تقييم");
            }
            else if (totalCases >= 10)
            {
                score += 7;
            }
            else
            {
                score += 3;
            }

            return (Math.Min(score, 100), string.Join(" | ", reasons));
        }
    }
}