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
    // 3. تحديث غلاف الملف
    public class UpdateCoverPictureDto
    {
        [Required(ErrorMessage = "صورة الغلاف مطلوبة")]
        public IFormFile CoverPicture { get; set; } = null!;
    }
}

