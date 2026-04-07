using System;
using System.Collections.Generic;

namespace LegalMateAI.Domain.Entities
{
    public class Admin
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        // public DateTime? UpdatedAt { get; set; }  // Add this property
        // public bool IsActive { get; set; } = true;
        
        // العلاقات
        public AdminProfile? Profile { get; set; }
        public ICollection<AdminLog> AdminLogs { get; set; } = new List<AdminLog>();
    }
}