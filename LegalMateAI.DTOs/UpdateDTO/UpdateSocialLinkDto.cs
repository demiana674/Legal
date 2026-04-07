using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
namespace LegalMateAI.DTOs.UpdateDTO
{
    // 5. تحديث رابط التواصل
    public class UpdateSocialLinkDto
    {
        public SocialPlatform? Platform { get; set; }
        public string? Url { get; set; }
        public string? Username { get; set; }
        public bool? IsPublic { get; set; }
    }
}