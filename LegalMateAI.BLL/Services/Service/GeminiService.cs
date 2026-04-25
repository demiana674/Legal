using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.ReadDTO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class GeminiService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly double _temperature;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _apiKey = configuration["Gemini:ApiKey"] 
                ?? throw new ArgumentNullException("Gemini:ApiKey مطلوب في appsettings.json");
            
            _model = configuration["Gemini:Model"] ?? "gemini-pro";
            
            _maxTokens = configuration.GetValue<int>("Gemini:MaxTokens", 2000);
            _temperature = configuration.GetValue<double>("Gemini:Temperature", 0.7);
            _logger = logger;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
                Timeout = TimeSpan.FromSeconds(90)
            };
        }

        private string GetApiUrl()
        {
            if (_model == "gemini-pro")
            {
                return $"v1/models/{_model}:generateContent?key={_apiKey}";
            }
            return $"v1beta/models/{_model}:generateContent?key={_apiKey}";
        }

        private async Task<string?> CallGeminiAsync(string prompt, int maxTokens = 0)
        {
            try
            {
                var tokens = maxTokens > 0 ? maxTokens : _maxTokens;

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        maxOutputTokens = tokens,
                        temperature = _temperature,
                        topP = 0.95,
                        topK = 40
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(GetApiUrl(), httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {error}");
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    var candidate = candidates[0];
                    if (candidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString()?.Trim();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini API call failed");
                return null;
            }
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var result = await CallGeminiAsync("Say hello", 10);
                return !string.IsNullOrEmpty(result);
            }
            catch { return false; }
        }

        public async Task<AIAnalysisResult> AnalyzeDocumentAsync(Document document, byte[] fileContent)
        {
            var text = ExtractText(fileContent, document.FileName ?? "document");

            if (string.IsNullOrEmpty(text) || text.Length < 30)
            {
                return new AIAnalysisResult
                {
                    Summary = "النص قصير جداً للتحليل",
                    IsFallback = true
                };
            }

            text = text.Length > 8000 ? text[..8000] : text;

            var prompt = $@"أنت محلل قانوني خبير. حلل المستند التالي وأعطني JSON:

{{
  ""summary"": ""ملخص 3-5 جمل"",
  ""risks"": [{{""type"": ""نوع"", ""description"": ""وصف"", ""level"": ""High/Medium/Low"", ""suggestion"": ""اقتراح""}}],
  ""clauses"": [{{""title"": ""عنوان"", ""text"": ""نص"", ""importance"": ""High/Medium/Low""}}]
}}

المستند:
{text}";

            var result = await CallGeminiAsync(prompt, _maxTokens);

            if (string.IsNullOrEmpty(result))
            {
                return new AIAnalysisResult
                {
                    Summary = "فشل التحليل. يرجى المحاولة مرة أخرى.",
                    ExtractedText = text[..Math.Min(text.Length, 500)],
                    IsFallback = true
                };
            }

            return ParseAnalysisResult(result, text);
        }

        public async Task<QuickAnalysisResult> QuickAnalysisAsync(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 30)
            {
                return new QuickAnalysisResult
                {
                    RiskSummary = "النص قصير جداً",
                    RiskLevel = "غير معروف",
                    IsFallback = true
                };
            }

            text = text.Length > 4000 ? text[..4000] : text;

            var prompt = $"حلل المخاطر في هذا النص باختصار:\n\n{text}";
            var result = await CallGeminiAsync(prompt, 500);

            return new QuickAnalysisResult
            {
                RiskSummary = result ?? "فشل التحليل",
                RiskLevel = result != null && result.Contains("عالية") ? "عالية" : "متوسطة",
                RiskScore = result != null && result.Contains("خطر") ? 7 : 3,
                DetectedRisks = result?.Split('\n').Where(l => l.Contains("خطر") || l.Contains("⚠")).ToList() ?? new(),
                IsFallback = string.IsNullOrEmpty(result)
            };
        }

        public async Task<string> GenerateContractAsync(ContractTemplate template, Dictionary<string, string> data)
        {
            var dataJson = JsonSerializer.Serialize(data);
            var prompt = $"أنت محامٍ مصري. اصيغ عقد {template.Name} بالعربية:\n{dataJson}";
            return await CallGeminiAsync(prompt, 1500) ?? $"عقد {template.Name}\n\n{dataJson}";
        }

        public async Task<List<LawyerSuggestionDto>> SuggestLawyersAsync(string documentContent, string specialization)
        {
            return new List<LawyerSuggestionDto>
            {
                new LawyerSuggestionDto { LawyerName = "محامٍ متخصص", Specialization = specialization, MatchScore = 0.8 }
            };
        }

        public async Task<List<SearchResultDto>> SmartSearchAsync(string query, int limit = 10)
        {
            return new List<SearchResultDto>
            {
                new SearchResultDto { Title = "نتيجة بحث", Snippet = $"نتائج عن: {query}", Relevance = 0.9 }
            };
        }

        public async Task<string> ChatAsync(string userMessage, List<(string role, string content)>? history = null)
        {
            var systemPrompt = GetSystemPrompt();
            var fullPrompt = systemPrompt;

            if (history != null && history.Any())
            {
                foreach (var (role, content) in history.TakeLast(6))
                {
                    var roleLabel = role == "user" ? "المستخدم" : "المساعد";
                    fullPrompt += $"\n\n{roleLabel}: {content}";
                }
            }

            fullPrompt += $"\n\nالمستخدم: {userMessage}\n\nالمساعد:";

            return await CallGeminiAsync(fullPrompt, 1500) ?? 
                "عذراً، الخدمة غير متاحة حالياً. يرجى المحاولة مرة أخرى.";
        }

        private string ExtractText(byte[] fileContent, string fileName)
        {
            try
            {
                return Encoding.UTF8.GetString(fileContent);
            }
            catch { return ""; }
        }

        private AIAnalysisResult ParseAnalysisResult(string result, string text)
        {
            try
            {
                var cleaned = result.Replace("```json", "").Replace("```", "").Trim();
                using var doc = JsonDocument.Parse(cleaned);
                var root = doc.RootElement;

                var analysis = new AIAnalysisResult
                {
                    Summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : result,
                    ExtractedText = text[..Math.Min(text.Length, 500)],
                    Result = result,
                    IsFallback = false
                };

                if (root.TryGetProperty("risks", out var risks))
                {
                    analysis.Risks = JsonSerializer.Deserialize<List<AIRisk>>(risks.GetRawText()) ?? new();
                }

                if (root.TryGetProperty("clauses", out var clauses))
                {
                    analysis.Clauses = JsonSerializer.Deserialize<List<AIClause>>(clauses.GetRawText()) ?? new();
                }

                return analysis;
            }
            catch
            {
                return new AIAnalysisResult
                {
                    Summary = result[..Math.Min(result.Length, 500)],
                    ExtractedText = text[..Math.Min(text.Length, 500)],
                    Result = result,
                    IsFallback = false
                };
            }
        }

        public static string GetSystemPrompt()
        {
            return @"أنت ""المستشار"" - مساعد قانوني ذكي متخصص في القانون المصري.

هويتك: خبير في القانون المدني، التجاري، الجنائي، العمالي، الأحوال الشخصية.

✅ لغة عربية فصحى واضحة
✅ إجابات منظمة
✅ اشرح المصطلحات القانونية
✅ قل ""يُنصح باستشارة محامٍ متخصص"" للنصائح النهائية
❌ لا تقدم استشارات قانونية نهائية
❌ لا تخمن معلومات غير موجودة";
        }
    }
}