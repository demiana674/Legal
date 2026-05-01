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
    // 1. الملف الشخصي الكامل
    public class UserProfileResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        
        // الأساسيات
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string? ProfilePictureUrl { get; set; }
        public string? CoverPictureUrl { get; set; }
        
        // الاتصال
        public string? PhoneNumber { get; set; }
        public string? AlternativePhone { get; set; }
        public string? Email { get; set; }
        
        // الموقع
        public int? GovernorateId { get; set; }
        public string? GovernorateName { get; set; }
        public int? CityId { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        
        // الشخصية
        public DateTime? DateOfBirth { get; set; }
        public string? DateOfBirthFormatted => DateOfBirth?.ToString("dd MMM yyyy");
        // public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? NationalId { get; set; }
        public string? NationalIdMasked => MaskNationalId(NationalId);
        
        // المهنية
        public string? Bio { get; set; }
        public string? JobTitle { get; set; }
        public string? Company { get; set; }
        
        // الإحصائيات
        public int ProfileViews { get; set; }
        public int DocumentsCount { get; set; }
        public int SocialLinksCount { get; set; }
        
        // التواريخ
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => CreatedAt.ToString("dd MMM yyyy");
        public DateTime? UpdatedAt { get; set; }
        
        // العلاقات
        public List<UserDocumentDto> Documents { get; set; } = new();
 
        
        // دوال مساعدة
        private string MaskNationalId(string? id)
        {
            if (string.IsNullOrEmpty(id) || id.Length < 14)
                return id ?? string.Empty;
            
            return $"{id.Substring(0, 6)}****{id.Substring(10, 4)}";
        }
    }
}


