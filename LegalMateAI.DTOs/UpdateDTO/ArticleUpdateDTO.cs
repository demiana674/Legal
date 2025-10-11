using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class ArticleUpdateDTO
    {
        
        public string? Text { get; set; }
        public string? Notes { get; set; }
        public string? ArticleNumber { get; set; }
        
    }
}