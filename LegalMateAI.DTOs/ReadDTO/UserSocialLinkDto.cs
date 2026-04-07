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
    // 4. رابط التواصل
    public class UserSocialLinkDto
    {
        public Guid Id { get; set; }
        public SocialPlatform Platform { get; set; }
        public string PlatformName => Platform switch
        {
            SocialPlatform.Facebook => "فيسبوك",
            SocialPlatform.Twitter => "تويتر",
            SocialPlatform.LinkedIn => "لينكد إن",
            SocialPlatform.WhatsApp => "واتساب",
            SocialPlatform.Telegram => "تيليجرام",
            SocialPlatform.Website => "موقع شخصي",
            _ => Platform.ToString()
        };
        public string PlatformIcon => Platform switch
        {
            SocialPlatform.Facebook => "📘",
            SocialPlatform.Twitter => "🐦",
            SocialPlatform.LinkedIn => "🔗",
            SocialPlatform.WhatsApp => "📱",
            SocialPlatform.Telegram => "✈️",
            SocialPlatform.Website => "🌐",
            _ => "🔗"
        };
        public string Url { get; set; } = string.Empty;
        public string? Username { get; set; }
        public bool IsPublic { get; set; }
    }
}


