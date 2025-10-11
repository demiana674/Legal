using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class UserContractReadDTO
    {
        public int ContractID { get; set; }
        public int UserID { get; set; }
        public int TemplateID { get; set; }
        
        public DateTime CreatedDate { get; set; }

        public DateTime FilledDate { get; set; }

        public string? PdfUrl { get; set; }

    }
}