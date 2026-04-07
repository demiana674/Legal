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
        public class ContractTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ContractType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TemplateContent { get; set; } = string.Empty;
        public string[]? Placeholders { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; } 
    }
}