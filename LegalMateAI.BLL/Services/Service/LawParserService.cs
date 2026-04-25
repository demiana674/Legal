// LegalMateAI.BLL/Services/Service/LawParserService.cs
using System.Net;
using System.Text.RegularExpressions;
using LegalMateAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using LegalMateAI.DTOs.CreateDTO;
namespace LegalMateAI.BLL.Services.Service
{
    /// <summary>
    /// خدمة استخراج بيانات القانون من الرابط
    /// </summary>
    public class LawParserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LawParserService> _logger;

        public LawParserService(HttpClient httpClient, ILogger<LawParserService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// استخراج بيانات القانون من رابط manshurat.org
        /// </summary>
        public async Task<ParsedLawData> ParseFromUrlAsync(string url)
        {
            var result = new ParsedLawData
            {
                SourceUrl = url
            };

            try
            {
                _logger.LogInformation($"Parsing law from URL: {url}");
                
                var response = await _httpClient.GetStringAsync(url);
                var html = response;

                // ===== 1. استخراج اسم القانون =====
                result.Name = ExtractTitle(html, url);

                // ===== 2. استخراج سنة الإصدار =====
                result.Year = ExtractYear(html);

                // ===== 3. استخراج التصنيف =====
                result.Category = ExtractCategory(html);

                // ===== 4. استخراج رقم القانون (لو موجود) =====
                result.LawNumber = ExtractLawNumber(html, result.Name);

                // ===== 5. استخراج الوصف =====
                result.Description = ExtractDescription(html);

                // ===== 6. استخراج الكلمات المفتاحية (الوسوم) =====
                result.SearchKeywords = ExtractTags(html);

                // ===== 7. استخراج جهة الإصدار =====
                result.IssuingAuthority = ExtractIssuingAuthority(html);

                // ===== 8. استخراج تاريخ الإصدار =====
                result.IssueDate = ExtractIssueDate(html);

                // ===== 9. استخراج رابط PDF =====
                result.PdfUrl = ExtractPdfUrl(html, url);

                // ===== 10. استخراج نوع الوثيقة =====
                result.DocumentType = ExtractDocumentType(html);

                _logger.LogInformation($"Successfully parsed law: {result.Name}, Year: {result.Year}, Category: {result.Category}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to parse law from URL: {url}");
                // استخدم البيانات اللي المستخدم دخلها يدوياً
            }

            return result;
        }

        /// <summary>
        /// استخراج اسم القانون من العنوان أو الـ URL
        /// </summary>
        private string ExtractTitle(string html, string url)
        {
            // جرب نجيب من title tag
            var titleMatch = Regex.Match(html, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                var title = titleMatch.Groups[1].Value.Trim();
                // شيل اسم الموقع من العنوان
                title = Regex.Replace(title, @"\s*[-|]\s*منشورات\s*قانونية\s*.*$", "", RegexOptions.IgnoreCase);
                
                if (!string.IsNullOrEmpty(title) && title.Length > 5)
                    return title;
            }

            // جرب نجيب من h1
            var h1Match = Regex.Match(html, @"<h1[^>]*>([^<]+)</h1>", RegexOptions.IgnoreCase);
            if (h1Match.Success)
                return h1Match.Groups[1].Value.Trim();

            // جرب نجيب من page-title class
            var pageTitleMatch = Regex.Match(html, @"class=[""']page-title[""'][^>]*>([^<]+)<", RegexOptions.IgnoreCase);
            if (pageTitleMatch.Success)
                return pageTitleMatch.Groups[1].Value.Trim();

            // استخدم آخر جزء من الـ URL
            var uri = new Uri(url);
            var lastSegment = uri.Segments.LastOrDefault()?.Trim('/') ?? "";
            return lastSegment.Replace("-", " ").Replace("_", " ");
        }

        /// <summary>
        /// استخراج سنة الإصدار
        /// </summary>
        private int? ExtractYear(string html)
        {
            // جرب نجيب من "سنة الإصدار/السنة القضائية"
            var patterns = new[]
            {
                @"سنة الإصدار[:\s]*(\d{4})",
                @"سنة الإصدار/السنة القضائية[:\s]*(\d{4})",
                @"سنة\s*الإصدار[:\s]*(\d{4})",
                @"السنة[:\s]*(\d{4})",
                @"Year[:\s]*(\d{4})",
                @"تاريخ الإصدار[:\s]*(\d{4})",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var year))
                {
                    if (year >= 1800 && year <= DateTime.Now.Year + 5)
                        return year;
                }
            }

            return null;
        }

        /// <summary>
        /// استخراج التصنيف من نوع الوثيقة
        /// </summary>
        private LawCategory? ExtractCategory(string html)
        {
            var patterns = new Dictionary<LawCategory, string[]>
            {
                { LawCategory.Constitutional, new[] { "دستور", "دستورية", "دستوري" } },
                { LawCategory.Civil, new[] { "مدني", "مدنية" } },
                { LawCategory.Criminal, new[] { "جنائي", "عقوبات", "جنائية", "جنح" } },
                { LawCategory.Commercial, new[] { "تجاري", "تجارة", "شركات", "تجارية" } },
                { LawCategory.Labor, new[] { "عمل", "عمال", "عمالية" } },
                { LawCategory.Family, new[] { "أسرة", "أحوال شخصية", "حضانة", "نفقة" } },
                { LawCategory.Tax, new[] { "ضرائب", "ضريبة", "ضريبية" } },
                { LawCategory.Administrative, new[] { "إداري", "إدارية", "مجلس الدولة" } },
                { LawCategory.RealEstate, new[] { "عقاري", "إيجار", "عقارية" } },
                { LawCategory.Investment, new[] { "استثمار", "استثماري" } },
                { LawCategory.Financial, new[] { "مالي", "مصارف", "بنوك", "مالية" } },
            };

            var htmlLower = html.ToLower();
            
            foreach (var category in patterns)
            {
                foreach (var keyword in category.Value)
                {
                    if (htmlLower.Contains(keyword.ToLower()))
                        return category.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// استخراج رقم القانون
        /// </summary>
        private string? ExtractLawNumber(string html, string? title)
        {
            // جرب نجيب من النص "قانون رقم X لسنة Y"
            var patterns = new[]
            {
                @"قانون\s+رقم\s+(\d+)\s+لسنة",
                @"قرار\s+رقم\s+(\d+)\s+لسنة",
                @"رقم\s+(\d+)\s+لسنة",
                @"Law\s*No\.?\s*(\d+)",
                @"رقم\s*(\d+)",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[1].Value;
            }

            return null;
        }

        /// <summary>
        /// استخراج وصف القانون
        /// </summary>
        private string? ExtractDescription(string html)
        {
            // جرب نجيب من meta description
            var metaMatch = Regex.Match(html, @"<meta[^>]*name=[""']description[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (metaMatch.Success)
                return metaMatch.Groups[1].Value.Trim();

            // جرب نجيب أول فقرة طويلة
            var paragraphMatch = Regex.Match(html, @"<p[^>]*>([^<]{50,300})</p>", RegexOptions.IgnoreCase);
            if (paragraphMatch.Success)
                return paragraphMatch.Groups[1].Value.Trim();

            return null;
        }

        /// <summary>
        /// استخراج الوسوم (الكلمات المفتاحية)
        /// </summary>
        private string? ExtractTags(string html)
        {
            var tags = new List<string>();

            // جرب نجيب من "وسومـــــ"
            var tagsSection = Regex.Match(html, @"وسوم[ـ]*\s*</[^>]+>\s*<[^>]+>\s*((?:<a[^>]*>[^<]+</a>\s*)+)", RegexOptions.IgnoreCase);
            if (tagsSection.Success)
            {
                var tagMatches = Regex.Matches(tagsSection.Groups[1].Value, @"<a[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase);
                foreach (Match tag in tagMatches)
                {
                    var tagText = tag.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(tagText) && tagText != "Facebook" && tagText != "Twitter")
                        tags.Add(tagText);
                }
            }

            return tags.Count > 0 ? string.Join(",", tags.Distinct()) : null;
        }

        /// <summary>
        /// استخراج جهة الإصدار
        /// </summary>
        private string? ExtractIssuingAuthority(string html)
        {
            var patterns = new[]
            {
                @"صفة المصدر / جهة الإصدار[:\s]*<[^>]*>([^<]+)<",
                @"جهة الإصدار[:\s]*<[^>]*>([^<]+)<",
                @"اسم المصدر[:\s]*<[^>]*>([^<]+)<",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[1].Value.Trim();
            }

            return null;
        }

        /// <summary>
        /// استخراج تاريخ الإصدار
        /// </summary>
        private DateTime? ExtractIssueDate(string html)
        {
            var patterns = new[]
            {
                @"تاريخ إصدار الوثيقة / الحكم[:\s]*<[^>]*>([^<]+)<",
                @"تاريخ الإصدار[:\s]*<[^>]*>([^<]+)<",
                @"تاريخ النشر[:\s]*<[^>]*>([^<]+)<",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var dateStr = match.Groups[1].Value.Trim();
                    if (DateTime.TryParse(dateStr, out var date))
                        return date;
                }
            }

            return null;
        }

        /// <summary>
        /// استخراج رابط تحميل PDF
        /// </summary>
        private string? ExtractPdfUrl(string html, string sourceUrl)
        {
            // جرب نجيب رابط PDF مباشر
            var pdfMatch = Regex.Match(html, @"href=[""']([^""']+\.pdf)[""']", RegexOptions.IgnoreCase);
            if (pdfMatch.Success)
            {
                var pdfUrl = pdfMatch.Groups[1].Value;
                
                // لو الرابط نسبي، حوله لـ absolute
                if (!pdfUrl.StartsWith("http"))
                {
                    var baseUri = new Uri(sourceUrl);
                    pdfUrl = new Uri(baseUri, pdfUrl).ToString();
                }
                
                return pdfUrl;
            }

            // جرب نجيب من "Download" link
            var downloadMatch = Regex.Match(html, @"<a[^>]*href=[""']([^""']*)[""'][^>]*>\s*Download\s*</a>", RegexOptions.IgnoreCase);
            if (downloadMatch.Success)
            {
                var downloadUrl = downloadMatch.Groups[1].Value;
                if (!downloadUrl.StartsWith("http"))
                {
                    var baseUri = new Uri(sourceUrl);
                    downloadUrl = new Uri(baseUri, downloadUrl).ToString();
                }
                return downloadUrl;
            }

            return null;
        }

        /// <summary>
        /// استخراج نوع الوثيقة
        /// </summary>
        private string? ExtractDocumentType(string html)
        {
            var match = Regex.Match(html, @"نوع الوثيقة[:\s]*<[^>]*>([^<]+)<", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();
            return null;
        }
    }

    /// <summary>
    /// البيانات المستخرجة من الرابط
    /// </summary>
    public class ParsedLawData
    {
        public string? Name { get; set; }
        public string? LawNumber { get; set; }
        public int? Year { get; set; }
        public LawCategory? Category { get; set; }
        public string? Description { get; set; }
        public string? SearchKeywords { get; set; }
        public string? IssuingAuthority { get; set; }
        public DateTime? IssueDate { get; set; }
        public string? PdfUrl { get; set; }
        public string? DocumentType { get; set; }
        public string SourceUrl { get; set; } = string.Empty;

        /// <summary>
        /// دمج البيانات المستخرجة مع البيانات اللي دخلها المستخدم
        /// </summary>
        public void MergeWithUserInput(CreateLawRequestDto userInput)
        {
            Name ??= userInput.Name;
            LawNumber ??= userInput.LawNumber;
            Year ??= userInput.Year;
            Category ??= userInput.Category;
            Description ??= userInput.Description;
            SearchKeywords ??= userInput.SearchKeywords;
        }
    }
}