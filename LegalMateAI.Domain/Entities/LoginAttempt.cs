using System;

namespace LegalMateAI.Domain.Entities
{
    public class LoginAttempt
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? AdminId { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime AttemptedAt { get; set; }
        public bool IsSuccess { get; set; }
        
        // Navigation properties
        public User? User { get; set; }
        public Admin? Admin { get; set; }
    }
}