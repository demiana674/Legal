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
    // 1. إنشاء ملف شخصي (عادة بعد التسجيل)
    public class CreateUserProfileDto
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الأخير مطلوب")]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }

        public int? GovernorateId { get; set; }

        public string? City { get; set; }

        public string? Address { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [StringLength(14, MinimumLength = 14, ErrorMessage = "الرقم القومي يجب أن يكون 14 رقمًا")]
        public string? NationalId { get; set; }

        public string? JobTitle { get; set; }

        public string? Company { get; set; }

        public IFormFile? ProfilePicture { get; set; }
    }
}


