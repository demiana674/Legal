using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    public class IRQueries
    {
        [Key]
        public int QueryID { get; set; }
        [Required]
        public int UserID { get; set; }
        [Required]
        [MaxLength(2000)]
        public string QueryText { get; set; }= string.Empty;

        public DateTime QueriedAt { get; set; } = DateTime.UtcNow;

        public QueryStatus Status { get; set; } = QueryStatus.Pending;

        public string? MatchedDocuments { get; set; } 
        public User User { get; set; } = null!; // Navigation property for User
        public ICollection<IRQueryDocument> QueryDocuments { get; set; } = new List<IRQueryDocument>();
        //keywords for search
        public byte[]? EmbeddingVector { get; set; }
    }
    public enum QueryStatus
    {
        Pending,
        Completed,
        Failed
    }

}