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
    public class Certificate
    {
        public Guid Id { get; set; }
        public Guid LawyerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IssuingOrganization { get; set; } = string.Empty;
        public int Year { get; set; }
        public string? FileUrl { get; set; }
        
        public LawyerProfile Lawyer { get; set; } = null!;
    }
}