using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class AdminLog
    {
        public Guid Id { get; set; }
        public Guid AdminId { get; set; }
        public AdminLogAction Action { get; set; }
        public string TargetType { get; set; } = string.Empty; // "Lawyer", "User", "System"
        public Guid TargetId { get; set; }
        public DateTime Timestamp { get; set; }
        
        public Admin Admin { get; set; } = null!;
    }
}