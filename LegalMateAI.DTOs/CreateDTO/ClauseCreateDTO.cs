using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class ClauseCreateDTO
    {
       
        public int ArticleID { get; set; }
        public string ClauseText { get; set; }= string.Empty;
        public string? ClauseNumber { get; set; }
        public string? Explanation { get; set; }

    }
}