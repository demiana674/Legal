using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace LegalMateAI.DTOs.UpdateDTO
{
    // 2. تحديث صورة الملف
    public class UpdateProfilePictureDto
    {
        [Required(ErrorMessage = "الصورة مطلوبة")]
        public IFormFile ProfilePicture { get; set; } = null!;
    }
}

