// LegalMateAI.DTOs/ReadDTO/UserResponseDto.cs
using System;
using LegalMateAI.Domain.Enums;
using System.Text.Json.Serialization; 
namespace LegalMateAI.DTOs.ReadDTO
{
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
         public string? ProfilePicture { get; set; } 
        public string PhoneNumber { get; set; } = string.Empty;
         [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AlternativePhone { get; set; } 
        public string? NationalId { get; set; }
        
        public string? City { get; set; }
        public int? GovernorateId { get; set; }
        public string? GovernorateName { get; set; }
        public string? CityName { get; set; }
        public string? Address { get; set; }
        // public string? Gender { get; set; } 
        public string? Nationality { get; set; } 
        public UserRole Role { get; set; }
        public string RoleName => Role.ToString();
        public AccountStatus Status { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? DateOfBirth { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int DocumentsCount { get; set; }
        public int ContractsCount { get; set; }
        public int AppointmentsCount { get; set; }
        // public string? LastPasswordChange { get; set; }
    }
}