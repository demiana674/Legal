using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    [Index(nameof(Title))]
    [Index(nameof(Category))]
    public class Law
    {
        [Key]
        public int LawID { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        [Url]
        public string? SourceURL { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "General";
        [MaxLength(200)]
        public string? IssuedBy { get; set; }

        public ICollection<Articles> LawArticles { get; set; } = new List<Articles>();
        public ICollection<LawUpdates> LawUpdates { get; set; } = new List<LawUpdates>();

    }
}