using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.Domain.Entities
{
    public class Clause
    {
        [Key]
        public int ClauseID { get; set; }
        [Required]
        public int ArticleID { get; set; }
        [MaxLength(50)]
        public string? ClauseNumber { get; set; }

        [Required]
        public string ClauseText { get; set; } = string.Empty;
        public string? Explanation { get; set; }

        public Articles Article { get; set; } = null!;
    }
}