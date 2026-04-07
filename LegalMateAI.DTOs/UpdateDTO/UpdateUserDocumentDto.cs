using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
namespace LegalMateAI.DTOs.UpdateDTO
{
    // 4. تحديث مستند
    public class UpdateUserDocumentDto
    {
        public string? Description { get; set; }
        public UserDocumentType? DocumentType { get; set; }
        public IFormFile? NewDocument { get; set; }
    }
}

