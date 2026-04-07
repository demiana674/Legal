using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegalMateAI.Domain.Enums;
using LegalMateAI.Domain.Entities;
namespace LegalMateAI.Domain.Entities
{

    public class UserDocument
    {
        public Guid Id { get; set; }
        public Guid UserProfileId { get; set; }
        public UserProfile UserProfile { get; set; } = null!;
        
        public string DocumentName { get; set; } = string.Empty;
        public string DocumentUrl { get; set; } = string.Empty;
        public UserDocumentType DocumentType { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsVerified { get; set; }
    }
}
   