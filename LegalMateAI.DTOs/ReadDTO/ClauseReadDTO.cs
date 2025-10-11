using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ClauseReadDTO
    {
        public int ClauseID { get; set; }
        public int ArticleID { get; set; }
        public string ClauseText { get; set; } = string.Empty;
        public string? ClauseNumber { get; set; }
        public string? Explanation { get; set; }
        // Including related Article information
        public string? ArticleNumber { get; set; }
        public string? LawTitle { get; set; }
    }
}