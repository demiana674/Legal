using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
namespace LegalMateAI.DTOs.CreateDTO
{

 public class CreateLawyerDto : CreateUserDto
    {
        [Required(ErrorMessage = "رقم رخصة المحاماة مطلوب")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "جهة النقابة مطلوبة")]
        public string BarAssociation { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ إصدار الرخصة مطلوب")]
        [DataType(DataType.Date)]
        public DateTime LicenseIssueDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LicenseExpiryDate { get; set; }

        [Required(ErrorMessage = "درجة المزاولة مطلوبة")]
        public string PracticeDegree { get; set; } = string.Empty;

        [Range(0, 70, ErrorMessage = "سنوات الخبرة يجب أن تكون بين 0 و 70")]
        public int YearsOfExperience { get; set; }

        public string? Biography { get; set; }

        public string[]? Specializations { get; set; }

        public string[]? Courts { get; set; }

        public string[]? Languages { get; set; }

        [Required(ErrorMessage = "ملف الرخصة مطلوب")]
        public IFormFile LicenseDocument { get; set; } = null!; // IFormFile بدلاً من byte[]
        
        // صورة شخصية اختيارية
        public IFormFile? ProfilePicture { get; set; }
    }
}

