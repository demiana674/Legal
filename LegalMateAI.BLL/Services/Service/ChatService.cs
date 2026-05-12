// LegalMateAI.BLL/Services/Service/ChatService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.BLL.Services.IService; // هااام جدا

namespace LegalMateAI.BLL.Services.Service
{
    public class ChatService : IChatService
    {
        private readonly LegalMateDbContext _context;
        private readonly IAIService _aiService; // <-- استخدام ال Interface بدل GeminiService
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            LegalMateDbContext context,
            IAIService aiService, // <-- التغيير هنا
            ILogger<ChatService> logger)
        {
            _context = context;
            _aiService = aiService; // <-- وهنا
            _logger = logger;
        }

        public async Task<ChatResponseDto?> SendMessageAsync(Guid? userId, ChatRequestDto request)
        {
            try
            {
                var sessionId = request.SessionId ?? Guid.NewGuid().ToString("N");

                var history = await _context.Conversations
                    .Where(c => c.SessionId == sessionId && (userId == null || c.UserId == userId))
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(10)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                if (request.ClearHistory)
                {
                    _context.Conversations.RemoveRange(history);
                    await _context.SaveChangesAsync();
                    history.Clear();
                }

                var chatHistory = history
                    .SelectMany(h => new List<(string role, string content)>
                    {
                        ("user", h.UserMessage),
                        ("assistant", h.AssistantResponse)
                    })
                    .ToList();

                // استخدام ال Interface الجديد
                var aiResponse = await _aiService.ChatAsync(request.Message, chatHistory);

                var response = aiResponse ?? "عذراً، لم أتمكن من معالجة طلبك.";

                var conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    SessionId = sessionId,
                    UserMessage = request.Message,
                    AssistantResponse = response,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                return new ChatResponseDto
                {
                    Response = response,
                    SessionId = sessionId,
                    HistoryLength = history.Count + 1,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessageAsync");
                return new ChatResponseDto
                {
                    Response = "عذراً، حدث خطأ في معالجة طلبك.",
                    SessionId = request.SessionId ?? Guid.NewGuid().ToString("N"),
                    HistoryLength = 0,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<ConversationHistoryDto?> GetConversationHistoryAsync(Guid? userId, string sessionId)
        {
            var conversations = await _context.Conversations
                .Where(c => c.SessionId == sessionId && (userId == null || c.UserId == userId))
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            if (!conversations.Any()) return null;

            var messages = new List<ConversationMessageDto>();
            foreach (var conv in conversations)
            {
                messages.Add(new ConversationMessageDto { Role = "user", Content = conv.UserMessage, Timestamp = conv.CreatedAt });
                messages.Add(new ConversationMessageDto { Role = "assistant", Content = conv.AssistantResponse, Timestamp = conv.CreatedAt.AddMilliseconds(100) });
            }

            return new ConversationHistoryDto
            {
                SessionId = sessionId,
                Messages = messages,
                MessageCount = messages.Count
            };
        }

        public async Task<bool> ClearConversationHistoryAsync(Guid? userId, string sessionId)
        {
            var conversations = await _context.Conversations
                .Where(c => c.SessionId == sessionId && (userId == null || c.UserId == userId))
                .ToListAsync();

            if (!conversations.Any()) return false;

            _context.Conversations.RemoveRange(conversations);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string?> GetQuickResponseAsync(string message)
        {
            return await _aiService.ChatAsync(message);
        }
    }
}