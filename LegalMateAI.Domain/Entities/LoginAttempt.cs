using System;

namespace LegalMateAI.Domain.Entities
{
    public class LoginAttempt
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }      // عمود واحد فقط
    public string Email { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; }
    public bool IsSuccess { get; set; }
    public User? User { get; set; }       // Navigation property
}
}