using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class IRQueryDocumentReadDTO
    {
        public int QueryID { get; set; }
        public int DocumentID { get; set; }

        // Additional properties for better context
        public string? QueryText { get; set; }
        public string? DocumentTitle { get; set; }
    }
}