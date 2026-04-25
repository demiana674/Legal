using System;
using System.Collections.Generic;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class User
    {
        public Guid UserID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? NationalId { get; set; }
        public string? Nationality { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public UserRole Role { get; set; }
        public AccountStatus Status { get; set; } = AccountStatus.Pending;
        public bool IsActive { get; set; } = true;
        public bool EmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime JoinDate { get; set; }
        public string? ProfilePicture { get; set; }
        
        // Relationships
        public LawyerProfile? LawyerProfile { get; set; }
        public UserProfile? UserProfile { get; set; }
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}