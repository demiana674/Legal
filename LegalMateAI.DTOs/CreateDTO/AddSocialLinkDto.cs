using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
namespace LegalMateAI.DTOs.CreateDTO
{
    // 3. إضافة رابط تواصل
    public class AddSocialLinkDto
    {
        [Required(ErrorMessage = "المنصة مطلوبة")]
        public SocialPlatform Platform { get; set; }

        [Required(ErrorMessage = "الرابط مطلوب")]
        [Url(ErrorMessage = "الرابط غير صحيح")]
        public string Url { get; set; } = string.Empty;

        public string? Username { get; set; }

        public bool IsPublic { get; set; } = true;
    }
}

