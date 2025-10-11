using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LegalMateAI.Domain.Entities
{
    public class IRDocuments
    {
        [Key]
        public int DocumentID { get; set; }
        [Required]
        public int UserID { get; set; }
        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
 
        public DocumentType DocumentType { get; set; } = DocumentType.Other;
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

        public User User { get; set; }= null!; // Navigation property for User
        public ICollection<IRQueryDocument> DocumentQueries { get; set; }= new List<IRQueryDocument>();


        //keywords for search
        public byte[]? EmbeddingVector { get; set; }
    }
    public enum DocumentType
    {
        Contract,
        Case,
        CourtDecision,
        LegalArticle,
        Other
    }

    public enum DocumentStatus
    {
        Pending,
        Processed,
        Rejected
    }
}