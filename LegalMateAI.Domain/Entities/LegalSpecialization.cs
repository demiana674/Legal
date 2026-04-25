using LegalMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    public class LegalSpecialization
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // التخصصات المصرية
        public static List<LegalSpecialization> EgyptianSpecializations() => new()
        {
            new() { Name = "Civil Law", NameAr = "القانون المدني" },
            new() { Name = "Criminal Law", NameAr = "القانون الجنائي" },
            new() { Name = "Family Law", NameAr = "قانون الأحوال الشخصية" },
            new() { Name = "Commercial Law", NameAr = "القانون التجاري" },
            new() { Name = "Labor Law", NameAr = "قانون العمل" },
            new() { Name = "Administrative Law", NameAr = "القانون الإداري" },
            new() { Name = "Real Estate Law", NameAr = "القانون العقاري" },
            new() { Name = "Tax Law", NameAr = "قانون الضرائب" },
            new() { Name = "Constitutional Law", NameAr = "القانون الدستوري" },
            new() { Name = "International Law", NameAr = "القانون الدولي" },
        };
    }
}