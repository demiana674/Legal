using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
   public class Articles
    {
        [Key]
        public int ArticleID { get; set; }
        [Required]
        public int LawID { get; set; }
        
        public string? Notes { get; set; }
      
        [Required]
        [MaxLength(500)]
        public string ArticleNumber { get; set; } = string.Empty;

        [Required]
        public string Text { get; set; } = string.Empty;
        public Law Law { get; set; }= null!;
        public ICollection<Clause> Clauses { get; set; } = new List<Clause>();
    }
}