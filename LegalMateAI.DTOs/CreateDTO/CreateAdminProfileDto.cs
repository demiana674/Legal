using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.CreateDTO
{
    // 1. إنشاء ملف أدمن (بعد إنشاء حساب الأدمن)
    public class CreateAdminProfileDto
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }

        public int? GovernorateId { get; set; }

        public string? City { get; set; }

        public string? Department { get; set; }

        public string? JobTitle { get; set; }

        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        public string? EmployeeId { get; set; }

        public IFormFile? ProfilePicture { get; set; }
    }
}

