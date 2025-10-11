using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class IRDocumentUpdateDTO
    {
        
        public string? Title { get; set; }
        public string? Content { get; set; }

        public DocumentStatus? Status { get; set; }
        public DocumentType? DocumentType { get; set; }

    }
}