using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class IRDocumentReadDTO
    {
        public int DocumentID { get; set; }
        public string Title { get; set; }= string.Empty;
        public string? Content { get; set; }
        public DateTime UploadedAt { get; set; }
       
        public int UserID { get; set; }

        public DocumentStatus Status { get; set; }
        public DocumentType DocumentType { get; set; }


    }
}