using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegalMateAI.Domain.Enums;
using LegalMateAI.Domain.Entities;
namespace LegalMateAI.Domain.Entities
{
 
 // روابط التواصل الاجتماعي
    public class UserSocialLink
    {
        public Guid Id { get; set; }
        public Guid UserProfileId { get; set; }
        public UserProfile UserProfile { get; set; } = null!;
        
        public SocialPlatform Platform { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Username { get; set; }
        public bool IsPublic { get; set; } = true;
    }

}