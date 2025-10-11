using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;
namespace LegalMateAI.DTOs.UpdateDTO
{
    public class IRQueryUpdateDTO
    {
        public QueryStatus? Status { get; set; }

        public string? MatchedDocuments { get; set; }
    }
}