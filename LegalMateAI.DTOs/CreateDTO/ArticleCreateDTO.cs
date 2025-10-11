using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class ArticleCreateDTO
    {
       
        public int LawID { get; set; }
        public string Text { get; set; }= string.Empty;
        public string? Notes { get; set; }
        public string ArticleNumber { get; set; } = string.Empty;
        
    }
}