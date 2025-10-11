using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class UserContractCreateDTO
    {
        
        public int UserID { get; set; }
        public int TemplateID { get; set; }
        
        public string? FilledDate { get; set; }
        public string? PdfUrl { get; set; }
       
    }
}