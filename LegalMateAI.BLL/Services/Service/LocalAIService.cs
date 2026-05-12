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
    public class LocalAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly double _temperature;
        private readonly string _systemPrompt;
        private readonly ILogger<LocalAIService> _logger;

        public LocalAIService(HttpClient httpClient, IConfiguration configuration, ILogger<LocalAIService> logger)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["LocalAI:BaseUrl"] ?? "http://localhost:11434";
            _model = configuration["LocalAI:Model"] ?? "aya-expanse:8b";
            _maxTokens = configuration.GetValue<int>("LocalAI:MaxTokens", 200);
            _temperature = configuration.GetValue<double>("LocalAI:Temperature", 0.0);
            _systemPrompt = configuration["LocalAI:SystemPrompt"] ?? "أنت مساعد قانوني مصري.";
            _logger = logger;
        }

        #region Ollama API Calls

        private async Task<string?> CallOllamaGenerateAsync(string prompt, int maxTokens = 0)
        {
            try
            {
                var tokens = maxTokens > 0 ? maxTokens : _maxTokens;

                var requestBody = new
                {
                    model = _model,
                    system = _systemPrompt,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        num_predict = tokens,
                        temperature = _temperature,
                        top_p = 0.3,
                        repeat_penalty = 1.2
                    }
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/generate", requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Ollama API error: {response.StatusCode} - {error}");
                    return null;
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                
                if (jsonResponse.TryGetProperty("response", out var responseText))
                {
                    return responseText.GetString()?.Trim();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ollama API call failed");
                return null;
            }
        }

        #endregion

        #region IAIService Implementation

        public async Task<bool> HealthCheckAsync()
        {
            var result = await CallOllamaGenerateAsync("قل 'بخير' فقط", 10);
            return !string.IsNullOrEmpty(result);
        }

        public async Task<string> ChatAsync(string userMessage, List<(string role, string content)>? history = null)
        {
            var result = await CallOllamaGenerateAsync(userMessage, _maxTokens);
            return result ?? "عذراً، لم أتمكن من معالجة طلبك.";
        }

        public async Task<AIAnalysisResult> AnalyzeDocumentAsync(Document document, byte[] fileContent)
        {
            var text = ExtractText(fileContent, document.FileName ?? "document");

            if (string.IsNullOrEmpty(text) || text.Length < 30)
            {
                return new AIAnalysisResult { Summary = "النص قصير جداً أو تعذر استخراجه", IsFallback = true };
            }

            text = text.Length > 10000 ? text[..10000] : text;

            var prompt = $@"حلل المستند القانوني التالي بدقة. لا تخترع معلومات. إذا لم تكن متأكداً، قل 'غير متأكد'.

المستند:
{text}";

            var result = await CallOllamaGenerateAsync(prompt, 500);

            if (string.IsNullOrEmpty(result))
            {
                return new AIAnalysisResult { Summary = "فشل التحليل.", IsFallback = true };
            }

            return new AIAnalysisResult
            {
                Summary = result,
                ExtractedText = text[..Math.Min(text.Length, 500)],
                Result = result,
                IsFallback = false
            };
        }

        public async Task<QuickAnalysisResult> QuickAnalysisAsync(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 20)
                return new QuickAnalysisResult { RiskSummary = "النص غير كافٍ", IsFallback = true };

            text = text.Length > 5000 ? text[..5000] : text;
            
            var prompt = $"حلل المخاطر القانونية في هذا النص بإيجاز. لا تخترع. إذا غير متأكد قل 'غير متأكد':\n\n{text}";
            var result = await CallOllamaGenerateAsync(prompt, 200);

            if (string.IsNullOrEmpty(result))
            {
                return new QuickAnalysisResult { RiskSummary = "فشل التحليل", RiskLevel = "متوسطة", IsFallback = true };
            }

            return new QuickAnalysisResult { RiskSummary = result, RiskLevel = "متوسطة", IsFallback = false };
        }

        public async Task<string> GenerateContractAsync(ContractTemplate template, Dictionary<string, string> data)
        {
            var dataJson = JsonSerializer.Serialize(data);
            var prompt = $"صغ عقد {template.Name} قانوني بالعربية بناءً على: {dataJson}. لا تخترع بنود غير موجودة.";
            return await CallOllamaGenerateAsync(prompt, 500) ?? "عذراً، فشل توليد العقد.";
        }

        public async Task<List<LawyerSuggestionDto>> SuggestLawyersAsync(string documentContent, string specialization)
        {
            return new List<LawyerSuggestionDto>
            {
                new()
                {
                    LawyerId = Guid.NewGuid(),
                    LawyerName = "محامي متخصص",
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

        #endregion

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

        #endregion
    }
}