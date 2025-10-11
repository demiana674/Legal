using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ContractsTemplateReadDTO
    {
        public int TemplateID { get; set; }
        public string Title { get; set; }= string.Empty;
        public string? Type { get; set; }
        
        public DateTime CreatedDate { get; set; }= DateTime.UtcNow;

        public string Content { get; set; } = string.Empty;
        
    }
}