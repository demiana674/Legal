using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.ReadDTO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.BLL.Services.Service
{
    /// <summary>
    /// خدمة الذكاء الاصطناعي المتكاملة مع Python AI Service
    /// </summary>
    public class PythonAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly ILogger<PythonAIService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly int _timeoutSeconds;


        public PythonAIService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration, 
            ILogger<PythonAIService> logger)
        {
            _baseUrl = configuration["PythonAI:Url"] ?? "http://localhost:8000/api/v1";
            _apiKey = configuration["PythonAI:ApiKey"] ?? "legalmate-ai-secret-key-2024";
            _timeoutSeconds = configuration.GetValue<int>("PythonAI:TimeoutSeconds", 90);
            
            // ✅ استخدام HttpClient من الـ Factory
            _httpClient = httpClientFactory.CreateClient("PythonAI");
            
            _logger = logger;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public PythonAIService(IConfiguration configuration, ILogger<PythonAIService> logger)
        {
            _baseUrl = configuration["PythonAI:Url"] ?? "http://localhost:8000/api/v1";
            _apiKey = configuration["PythonAI:ApiKey"] ?? "legalmate-ai-secret-key-2024";
            _timeoutSeconds = configuration.GetValue<int>("PythonAI:TimeoutSeconds", 90);
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds + 5);
            _logger = logger;
            
            // ✅ إضافة API Key إلى الـ HttpClient بشكل دائم
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// التحقق من صحة الخدمة
        /// </summary>
        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync($"{_baseUrl}/health", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// تحليل مستند قانوني
        /// </summary>
        public async Task<AIAnalysisResult> AnalyzeDocumentAsync(Document document, byte[] fileContent)
        {
            if (!await HealthCheckAsync())
            {
                _logger.LogWarning("Python AI service is not available, using fallback");
                return GetFallbackAnalysisResult(document, "AI service unavailable");
            }

            try
            {
                _logger.LogInformation("Sending document to AI service: {FileName}", document.FileName);
                
                using var content = new MultipartFormDataContent();
                var fileContentStream = new ByteArrayContent(fileContent);
                fileContentStream.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    document.FileType ?? "application/pdf");
                content.Add(fileContentStream, "file", document.FileName);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
                var response = await _httpClient.PostAsync($"{_baseUrl}/analyze/file", content, cts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cts.Token);
                    _logger.LogError("AI service error: {StatusCode} - {Error}", response.StatusCode, error);
                    return GetFallbackAnalysisResult(document, $"AI service returned {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                var result = JsonSerializer.Deserialize<PythonAnalysisResponse>(json, _jsonOptions);
                
                if (result == null)
                {
                    return GetFallbackAnalysisResult(document, "Failed to parse AI response");
                }
                
                return new AIAnalysisResult
                {
                    Summary = result.Summary?.Summary ?? "تم تحليل المستند بنجاح",
                    ExtractedText = result.Metadata?.ExtractedText ?? "",
                    Result = $"تم تحليل المستند بنجاح. تم العثور على {result.Clauses?.Count ?? 0} بند مهم.",
                    Clauses = result.Clauses?.Select(c => new AIClause
                    {
                        Title = c.Title ?? $"بند {Array.IndexOf(result.Clauses.ToArray(), c) + 1}",
                        Text = c.Text ?? "",
                        PageNumber = c.PageNumber ?? 1,
                        Importance = c.Importance ?? "Medium"
                    }).ToList() ?? new List<AIClause>(),
                    Risks = result.Risk?.RiskDetails?.Select(r => new AIRisk
                    {
                        Type = r.Type ?? "غير محدد",
                        Description = $"تم رصد خطر: {r.Type}",
                        Level = MapRiskLevel(r.Level),
                        Suggestion = r.Suggestion ?? "يُنصح بمراجعة محامٍ متخصص"
                    }).ToList() ?? new List<AIRisk>(),
                    IsFallback = false
                };
            }
            catch (TaskCanceledException)
            {
                return GetFallbackAnalysisResult(document, $"AI service timeout after {_timeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI service");
                return GetFallbackAnalysisResult(document, ex.Message);
            }
        }

        /// <summary>
        /// الدردشة مع المستند (RAG)
        /// </summary>
        // public async Task<ChatWithDocumentResponse> ChatWithDocumentAsync(string text, string question)
        // {
        //     if (!await HealthCheckAsync())
        //     {
        //         return new ChatWithDocumentResponse
        //         {
        //             Answer = "عذراً، خدمة الذكاء الاصطناعي غير متاحة حالياً.",
        //             Confidence = "منخفضة",
        //             IsFallback = true
        //         };
        //     }

        //     try
        //     {
        //         using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
        //         var response = await _httpClient.PostAsJsonAsync(
        //             $"{_baseUrl}/chat",
        //             new { text, question },
        //             _jsonOptions,
        //             cts.Token);
                
        //         if (!response.IsSuccessStatusCode)
        //         {
        //             return new ChatWithDocumentResponse
        //             {
        //                 Answer = "عذراً، حدث خطأ في خدمة المحادثة.",
        //                 Confidence = "منخفضة",
        //                 IsFallback = true
        //             };
        //         }
                
        //         var result = await response.Content.ReadFromJsonAsync<PythonChatResponse>(_jsonOptions, cts.Token);
                
        //         return new ChatWithDocumentResponse
        //         {
        //             Answer = result?.Answer ?? "لم أتمكن من العثور على إجابة مناسبة.",
        //             Confidence = result?.Confidence ?? "متوسطة",
        //             RelevantContext = result?.RelevantContext ?? "",
        //             RetrievedChunksCount = result?.RetrievedChunksCount ?? 0,
        //             TopScore = result?.TopScore ?? 0,
        //             IsFallback = false
        //         };
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error in chat with document");
        //         return new ChatWithDocumentResponse
        //         {
        //             Answer = "عذراً، حدث خطأ في خدمة المحادثة.",
        //             Confidence = "منخفضة",
        //             IsFallback = true
        //         };
        //     }
        // }

        /// <summary>
        /// بحث ذكي
        /// </summary>
        public async Task<List<DTOs.ReadDTO.SearchResultDto>> SmartSearchAsync(string query, int limit = 10)
        {
            if (!await HealthCheckAsync())
            {
                return new List<DTOs.ReadDTO.SearchResultDto>();
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_baseUrl}/search",
                    new { query, limit },
                    _jsonOptions,
                    cts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Search service returned {StatusCode}", response.StatusCode);
                    return new List<DTOs.ReadDTO.SearchResultDto>();
                }
                
                var results = await response.Content.ReadFromJsonAsync<List<PythonSearchResult>>(_jsonOptions, cts.Token);
                
                if (results == null || !results.Any())
                {
                    return new List<DTOs.ReadDTO.SearchResultDto>();
                }
                
                return results.Select(r => new DTOs.ReadDTO.SearchResultDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = !string.IsNullOrEmpty(r.Content) 
                        ? (r.Content.Length > 100 ? r.Content.Substring(0, 100) + "..." : r.Content)
                        : "نتيجة بحث",
                    Snippet = !string.IsNullOrEmpty(r.Content) 
                        ? (r.Content.Length > 200 ? r.Content.Substring(0, 200) + "..." : r.Content)
                        : "",
                    Relevance = r.Score ?? 0.5,
                    Type = "Document",
                    Date = DateTime.Now,
                    Url = $"/search/result/{Guid.NewGuid()}"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in smart search");
                return new List<DTOs.ReadDTO.SearchResultDto>();
            }
        }

        /// <summary>
        /// تحليل سريع
        /// </summary>
        public async Task<QuickAnalysisResult> QuickAnalysisAsync(string text)
        {
            if (!await HealthCheckAsync())
            {
                return new QuickAnalysisResult
                {
                    RiskSummary = "خدمة الذكاء الاصطناعي غير متاحة حالياً.",
                    RiskLevel = "متوسطة",
                    IsFallback = true
                };
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_baseUrl}/analyze/quick",
                    new { text },
                    _jsonOptions,
                    cts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new QuickAnalysisResult
                    {
                        RiskSummary = "حدث خطأ في التحليل السريع",
                        RiskLevel = "غير معروف",
                        IsFallback = true
                    };
                }
                
                var result = await response.Content.ReadFromJsonAsync<PythonQuickAnalysisResponse>(_jsonOptions, cts.Token);
                
                return new QuickAnalysisResult
                {
                    RiskSummary = result?.RiskSummary ?? "لم يتم اكتشاف مخاطر واضحة",
                    ClauseSummary = result?.ClauseSummary ?? "لم يتم استخراج بنود مهمة",
                    RiskLevel = result?.RiskLevel ?? "متوسطة",
                    RiskScore = result?.RiskScore ?? 0,
                    DetectedRisks = result?.DetectedRisks ?? new List<string>(),
                    IsFallback = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick analysis");
                return new QuickAnalysisResult
                {
                    RiskSummary = "حدث خطأ في التحليل السريع",
                    RiskLevel = "غير معروف",
                    IsFallback = true
                };
            }
        }

        /// <summary>
        /// إنشاء عقد باستخدام AI
        /// </summary>
        public async Task<string> GenerateContractAsync(ContractTemplate template, Dictionary<string, string> data)
        {
            if (!await HealthCheckAsync())
            {
                return GenerateFallbackContract(template, data);
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_baseUrl}/generate-contract",
                    new { template_id = template.Id.ToString(), data },
                    _jsonOptions,
                    cts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    return GenerateFallbackContract(template, data);
                }
                
                var result = await response.Content.ReadFromJsonAsync<PythonContractResponse>(_jsonOptions, cts.Token);
                
                return result?.Content ?? GenerateFallbackContract(template, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contract");
                return GenerateFallbackContract(template, data);
            }
        }

        /// <summary>
        /// اقتراح محامين
        /// </summary>
        public async Task<List<DTOs.ReadDTO.LawyerSuggestionDto>> SuggestLawyersAsync(string documentContent, string specialization)
        {
            if (!await HealthCheckAsync())
            {
                return new List<DTOs.ReadDTO.LawyerSuggestionDto>();
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_baseUrl}/suggest-lawyers",
                    new { content = documentContent, specialization },
                    _jsonOptions,
                    cts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new List<DTOs.ReadDTO.LawyerSuggestionDto>();
                }
                
                var result = await response.Content.ReadFromJsonAsync<List<PythonLawyerSuggestion>>(_jsonOptions, cts.Token);
                
                return result?.Select(r => new DTOs.ReadDTO.LawyerSuggestionDto
                {
                    LawyerId = Guid.NewGuid(),
                    LawyerName = r.Name ?? "محامي متخصص",
                    Specialization = r.Specialization ?? specialization,
                    Rating = r.Rating ?? 4.5,
                    MatchScore = r.MatchScore ?? 0.7,
                    CasesCount = r.CasesCount ?? 0
                }).ToList() ?? new List<DTOs.ReadDTO.LawyerSuggestionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suggesting lawyers");
                return new List<DTOs.ReadDTO.LawyerSuggestionDto>();
            }
        }

        #region Private Methods

        private RiskLevel MapRiskLevel(string? level)
        {
            return level?.ToLower() switch
            {
                "high" or "عالية" => RiskLevel.High,
                "medium" or "متوسطة" => RiskLevel.Medium,
                "low" or "منخفضة" => RiskLevel.Low,
                "critical" or "حرجة" => RiskLevel.Critical,
                _ => RiskLevel.Medium
            };
        }

        private AIAnalysisResult GetFallbackAnalysisResult(Document document, string errorReason)
        {
            return new AIAnalysisResult
            {
                Summary = "خدمة الذكاء الاصطناعي غير متاحة حالياً. هذا تحليل مبسط.",
                ExtractedText = "",
                Result = $"تم تحليل المستند باستخدام النظام البديل. السبب: {errorReason}",
                Clauses = new List<AIClause>
                {
                    new AIClause
                    {
                        Title = "تنبيه",
                        Text = "خدمة الذكاء الاصطناعي غير متاحة حالياً. يرجى المحاولة مرة أخرى لاحقاً.",
                        PageNumber = 1,
                        Importance = "High"
                    }
                },
                Risks = new List<AIRisk>
                {
                    new AIRisk
                    {
                        Type = "تحذير",
                        Description = "خدمة الذكاء الاصطناعي غير متاحة حالياً",
                        Level = RiskLevel.Medium,
                        Suggestion = "يرجى المحاولة مرة أخرى لاحقاً"
                    }
                },
                IsFallback = true
            };
        }

        private string GenerateFallbackContract(ContractTemplate template, Dictionary<string, string> data)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {template.Name}");
            sb.AppendLine();
            sb.AppendLine("**تنبيه:** خدمة الذكاء الاصطناعي غير متاحة حالياً. هذا عقد أساسي يحتاج إلى مراجعة محامٍ.");
            sb.AppendLine();
            
            foreach (var kvp in data)
            {
                sb.AppendLine($"**{kvp.Key}**: {kvp.Value}");
            }
            
            sb.AppendLine();
            sb.AppendLine("هذا عقد تم إنشاؤه بواسطة النظام. يرجى مراجعة المحامي قبل التوقيع.");
            
            return sb.ToString();
        }

        #endregion
    }

    #region Python API Response DTOs

    internal class PythonAnalysisResponse
    {
        public PythonSummary? Summary { get; set; }
        public List<PythonClause>? Clauses { get; set; }
        public PythonRisk? Risk { get; set; }
        public PythonMetadata? Metadata { get; set; }
    }

    internal class PythonSummary
    {
        public string? Summary { get; set; }
        public List<string>? KeyPoints { get; set; }
        public string? DocumentType { get; set; }
        public int? EstimatedPages { get; set; }
    }

    internal class PythonClause
    {
        public string? Title { get; set; }
        public string? Text { get; set; }
        public int? PageNumber { get; set; }
        public string? Importance { get; set; }
    }

    internal class PythonRisk
    {
        public int RiskScore { get; set; }
        public string? RiskLevel { get; set; }
        public List<PythonRiskDetail>? RiskDetails { get; set; }
    }

    internal class PythonRiskDetail
    {
        public string? Type { get; set; }
        public string? Level { get; set; }
        public string? Suggestion { get; set; }
    }

    internal class PythonMetadata
    {
        public string? ExtractedText { get; set; }
        public int TextLength { get; set; }
    }

    internal class PythonChatResponse
    {
        public string? Answer { get; set; }
        public string? Confidence { get; set; }
        public string? RelevantContext { get; set; }
        public int RetrievedChunksCount { get; set; }
        public double TopScore { get; set; }
    }

    internal class PythonSearchResult
    {
        public string? Content { get; set; }
        public double? Score { get; set; }
    }

    internal class PythonQuickAnalysisResponse
    {
        public string? RiskSummary { get; set; }
        public string? ClauseSummary { get; set; }
        public string? RiskLevel { get; set; }
        public int RiskScore { get; set; }
        public List<string>? DetectedRisks { get; set; }
    }

    internal class PythonContractResponse
    {
        public string? Content { get; set; }
    }

    internal class PythonLawyerSuggestion
    {
        public string? Name { get; set; }
        public string? Specialization { get; set; }
        public double? Rating { get; set; }
        public double? MatchScore { get; set; }
        public int? CasesCount { get; set; }
    }

    #endregion
}