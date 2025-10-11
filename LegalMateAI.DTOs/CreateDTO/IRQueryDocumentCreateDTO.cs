using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class IRQueryDocumentCreateDTO
    {
        [Required]
        public int QueryID { get; set; }

        [Required]
        public int DocumentID { get; set; }
    }
}