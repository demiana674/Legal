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

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] 
                ?? throw new ArgumentNullException("Gemini:ApiKey is required");

            _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
            _maxTokens = configuration.GetValue<int>("Gemini:MaxTokens", 4096);
            _temperature = configuration.GetValue<double>("Gemini:Temperature", 0.7);
            _logger = logger;
        }

        private string GetApiUrl()
        {
            return $"v1beta/models/{_model}:generateContent?key={_apiKey}";
        }

        private async Task<string?> CallGeminiAsync(string prompt, int maxTokens = 0, bool isJsonResponse = false)
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
                            parts = new[] { new { text = prompt } }
                        }
                    },
                    generationConfig = new
                    {
                        maxOutputTokens = tokens,
                        temperature = _temperature,
                        topP = 0.95,
                        responseMimeType = isJsonResponse ? "application/json" : "text/plain"
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(GetApiUrl(), requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {error}");
                    return null;
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                
                if (jsonResponse.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
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
            var result = await CallGeminiAsync("Say hello", 10);
            return !string.IsNullOrEmpty(result);
        }

        public async Task<AIAnalysisResult> AnalyzeDocumentAsync(Document document, byte[] fileContent)
        {
            var text = ExtractText(fileContent, document.FileName ?? "document");

            if (string.IsNullOrEmpty(text) || text.Length < 30)
            {
                return new AIAnalysisResult { Summary = "النص قصير جداً أو تعذر استخراجه", IsFallback = true };
            }

            text = text.Length > 15000 ? text[..15000] : text;

            var prompt = $@"أنت محلل قانوني مصري خبير. حلل المستند التالي بدقة واستخرج النتائج بصيغة JSON فقط:

{{
  ""summary"": ""ملخص شامل"",
  ""risks"": [{{""type"": ""نوع الخطر"", ""description"": ""وصف دقيق"", ""level"": ""High/Medium/Low"", ""suggestion"": ""كيفية المعالجة""}}],
  ""clauses"": [{{""title"": ""البند"", ""text"": ""محتواه"", ""importance"": ""High/Medium/Low""}}]
}}

المستند:
{text}";

            var result = await CallGeminiAsync(prompt, _maxTokens, isJsonResponse: true);

            if (string.IsNullOrEmpty(result))
            {
                return new AIAnalysisResult { Summary = "فشل التحليل التقني.", IsFallback = true };
            }

            return ParseAnalysisResult(result, text);
        }

        public async Task<QuickAnalysisResult> QuickAnalysisAsync(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 20)
                return new QuickAnalysisResult { RiskSummary = "النص غير كافٍ", IsFallback = true };

            text = text.Length > 15000 ? text[..15000] : text;
            var prompt = $"حلل المخاطر القانونية باختصار شديد في هذا النص واعرض النتيجة كـ JSON مع الحقول: RiskSummary, RiskLevel, RiskScore, DetectedRisks (array):\n\n{text}";
            var result = await CallGeminiAsync(prompt, 500, isJsonResponse: true);

            if (string.IsNullOrEmpty(result))
            {
                return new QuickAnalysisResult { RiskSummary = "فشل التحليل", RiskLevel = "متوسطة", IsFallback = true };
            }

            return ParseQuickAnalysisResult(result);
        }

        public async Task<string> GenerateContractAsync(ContractTemplate template, Dictionary<string, string> data)
        {
            var dataJson = JsonSerializer.Serialize(data);
            var prompt = $"أنت محامٍ مصري خبير. قم بصياغة عقد {template.Name} قانوني واحترافي بالعربية بناءً على البيانات التالية:\n{dataJson}";
            return await CallGeminiAsync(prompt, 2000) ?? "عذراً، فشل توليد العقد.";
        }

        public async Task<string> ChatAsync(string userMessage, List<(string role, string content)>? history = null)
        {
            StringBuilder chatPrompt = new StringBuilder(GetSystemPrompt());

            if (history != null)
            {
                foreach (var (role, content) in history.TakeLast(10))
                {
                    chatPrompt.Append($"\n{(role == "user" ? "المستخدم" : "المستشار")}: {content}");
                }
            }

            chatPrompt.Append($"\nالمستخدم: {userMessage}\nالمستشار:");

            return await CallGeminiAsync(chatPrompt.ToString(), 1500) 
                ?? "أعتذر، واجهت مشكلة في الاتصال بالخادم القانوني.";
        }

        public async Task<List<LawyerSuggestionDto>> SuggestLawyersAsync(string documentContent, string specialization)
        {
            var prompt = $"اقترح نوع المحامي المناسب لتحليل هذا المستند القانوني. التخصص المطلوب: {specialization}.\n\nالمستند:\n{documentContent[..1000]}";
            var result = await CallGeminiAsync(prompt, 200);
            
            return new List<LawyerSuggestionDto>
            {
                new()
                {
                    LawyerId = Guid.NewGuid(),
                    LawyerName = result ?? "محامي متخصص",
                    Specialization = specialization,
                    Rating = 4.5,
                    MatchScore = 0.8,
                    CasesCount = 10
                }
            };
        }

        public async Task<List<SearchResultDto>> SmartSearchAsync(string query, int limit = 10)
        {
            return new List<SearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = $"نتيجة بحث عن: {query[..Math.Min(query.Length, 50)]}",
                    Snippet = "استخدم محامٍ متخصص للحصول على نتائج دقيقة",
                    Relevance = 0.7,
                    Type = "Legal",
                    Date = DateTime.Now
                }
            };
        }

        #region Helpers

        private string ExtractText(byte[] fileContent, string fileName)
        {
            try
            {
                return Encoding.UTF8.GetString(fileContent);
            }
            catch
            {
                return "";
            }
        }

        private AIAnalysisResult ParseAnalysisResult(string result, string text)
        {
            try
            {
                var cleanedJson = result.Replace("```json", "").Replace("```", "").Trim();
                var analysis = JsonSerializer.Deserialize<AIAnalysisResult>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                analysis.ExtractedText = text[..Math.Min(text.Length, 500)];
                analysis.Result = result;
                analysis.IsFallback = false;
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Gemini JSON response");
                return new AIAnalysisResult { Summary = result, IsFallback = false };
            }
        }

        private QuickAnalysisResult ParseQuickAnalysisResult(string result)
        {
            try
            {
                var cleanedJson = result.Replace("```json", "").Replace("```", "").Trim();
                return JsonSerializer.Deserialize<QuickAnalysisResult>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch
            {
                return new QuickAnalysisResult { RiskSummary = result, RiskLevel = "متوسطة" };
            }
        }

        public static string GetSystemPrompt() => 
            @"أنت 'المستشار' - مساعد قانوني ذكي متخصص في القانون المصري. إجاباتك دقيقة، رسمية، وتستخدم المصطلحات القانونية المصرية الصحيحة. دائماً تنصح باستشارة محامٍ في النهاية.";

        #endregion
    }
}