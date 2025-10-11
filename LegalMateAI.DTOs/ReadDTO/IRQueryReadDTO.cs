using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class IRQueryReadDTO
    {
        public int QueryID { get; set; }
        public int UserID { get; set; }
        
        public string? QueryText { get; set; }
      
        public DateTime QueriedAt { get; set; }
        public QueryStatus Status { get; set; }
        public string? MatchedDocuments { get; set; }
       
    }
}
