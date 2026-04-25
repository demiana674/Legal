// LegalMateAI.BLL/Services/IService/IChatService.cs
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface IChatService
    {
        /// <summary>
        /// إرسال رسالة للمساعد الذكي
        /// </summary>
        Task<ChatResponseDto?> SendMessageAsync(Guid? userId, ChatRequestDto request);
        
        /// <summary>
        /// جلب تاريخ المحادثة
        /// </summary>
        Task<ConversationHistoryDto?> GetConversationHistoryAsync(Guid? userId, string sessionId);
        
        /// <summary>
        /// مسح تاريخ المحادثة
        /// </summary>
        Task<bool> ClearConversationHistoryAsync(Guid? userId, string sessionId);
        
        /// <summary>
        /// الحصول على رد سريع (بدون حفظ)
        /// </summary>
        Task<string?> GetQuickResponseAsync(string message);
    }
}