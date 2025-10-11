using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    
        public class IRQueryDocument
        {
        [ForeignKey("Query")]
        public int QueryID { get; set; }
        public IRQueries Query { get; set; }= null!;
        [ForeignKey("Document")]
        public int DocumentID { get; set; }
      public IRDocuments Document { get; set; } = null!;

           
}

    
}