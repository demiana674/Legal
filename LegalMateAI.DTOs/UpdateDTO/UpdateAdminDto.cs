using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.UpdateDTO
{
    // 2. تحديث أدمن
    public class UpdateAdminDto
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        // public AdminRole? Role { get; set; }
        // public AdminStatus? Status { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}

