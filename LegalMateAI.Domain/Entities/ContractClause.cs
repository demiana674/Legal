using LegalMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
 
    public class ContractClause
    {
        public Guid Id { get; set; }
        public Guid ContractId { get; set; }
        public string ClauseTitle { get; set; } = string.Empty;
        public string ClauseContent { get; set; } = string.Empty;
        public int Order { get; set; }
        
        public Contract Contract { get; set; } = null!;
    }
}