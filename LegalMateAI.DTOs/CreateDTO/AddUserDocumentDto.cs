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
    // 2. إضافة مستند
    public class AddUserDocumentDto
    {
        [Required(ErrorMessage = "الملف مطلوب")]
        public IFormFile Document { get; set; } = null!;

        [Required(ErrorMessage = "نوع المستند مطلوب")]
        public UserDocumentType DocumentType { get; set; }

        public string? Description { get; set; }
    }
}
