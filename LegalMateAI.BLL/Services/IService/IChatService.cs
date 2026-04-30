// LegalMateAI.BLL/Services/IService/IChatService.cs
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IChatService
    {
        Task<ChatResponseDto?> SendMessageAsync(Guid? userId, ChatRequestDto request);
        Task<ConversationHistoryDto?> GetConversationHistoryAsync(Guid? userId, string sessionId);
        Task<bool> ClearConversationHistoryAsync(Guid? userId, string sessionId);
        Task<string?> GetQuickResponseAsync(string message);
    }
}