using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    // 3. Login Response
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponseDto User { get; set; } = null!;
        public LawyerResponseDto? LawyerProfile { get; set; }
        public List<string> Permissions { get; set; } = new();
    }
}