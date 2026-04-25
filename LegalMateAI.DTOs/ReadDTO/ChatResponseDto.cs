using System;
using System.Collections.Generic;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ChatResponseDto
    {
        public string Response { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public int HistoryLength { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class ConversationHistoryDto
    {
        public string SessionId { get; set; } = string.Empty;
        public List<ConversationMessageDto> Messages { get; set; } = new();
        public int MessageCount { get; set; }
    }
    
    public class ConversationMessageDto
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}