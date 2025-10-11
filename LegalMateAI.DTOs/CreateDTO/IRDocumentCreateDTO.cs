using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class IRDocumentCreateDTO
    {
        [Required, MaxLength(200)]
        public string Title { get; set; }= string.Empty;
        public string? Content { get; set; }
        [Required]
        public DocumentType DocumentType { get; set; } = DocumentType.Other;
        [Required]
        public int UserID { get; set; }
    }
 
}