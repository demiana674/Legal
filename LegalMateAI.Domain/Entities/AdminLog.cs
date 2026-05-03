using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class AdminLog
    {
        public Guid Id { get; set; }

        public Guid ActorId { get; set; }   // أي شخص عمل الإجراء

        public string ActorName { get; set; } = string.Empty;

        public string ActorRole { get; set; } = string.Empty;

        public AdminLogAction Action { get; set; }

        public string TargetType { get; set; } = string.Empty;

        public Guid TargetId { get; set; }

        public DateTime Timestamp { get; set; }
    }
}