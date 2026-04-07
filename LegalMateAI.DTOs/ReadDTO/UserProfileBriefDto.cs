using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 2. ملف شخصي مختصر (للقوائم)
    public class UserProfileBriefDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? JobTitle { get; set; }
        public string? Company { get; set; }
        public string? Governorate { get; set; }
        public string Initials => GetInitials(FullName);
        
        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";
            var parts = name.Split(' ');
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}";
            return name.Length >= 2 ? name.Substring(0, 2) : name;
        }
    }
}


