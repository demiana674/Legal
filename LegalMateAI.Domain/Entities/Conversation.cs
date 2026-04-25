// LegalMateAI.Domain/Entities/Conversation.cs
using System;

namespace LegalMateAI.Domain.Entities
{
    public class Conversation
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string UserMessage { get; set; } = string.Empty;
        public string AssistantResponse { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? TokensUsed { get; set; }
        public string? ModelUsed { get; set; }
    }
}