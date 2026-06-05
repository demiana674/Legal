// LegalMateAI.Domain/Entities/LawyerSpecialty.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMateAI.Domain.Entities
{
    public class LawyerSpecialty
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }  // ✅ تغيير من Guid إلى int
        
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        
        public ICollection<LawyerProfileSpecialty> LawyerProfiles { get; set; } = new List<LawyerProfileSpecialty>();
        
        public static List<LawyerSpecialty> EgyptianLawyerSpecialties() => new()
        {
            new() { Name = "Personal Status", NameAr = "أحوال شخصية", Description = "قضايا الطلاق والحضانة والنفقة" },
            new() { Name = "Criminal", NameAr = "جنائي", Description = "قضايا جنائية ومخالفات" },
            new() { Name = "Civil", NameAr = "مدني", Description = "قضايا مدنية وتعويضات" },
            new() { Name = "Commercial", NameAr = "تجاري", Description = "قضايا تجارية وشركات" },
            new() { Name = "Labor", NameAr = "عمالي", Description = "قضايا عمل وعمال" },
            new() { Name = "Real Estate", NameAr = "عقاري", Description = "قضايا عقارية وملكية" },
            new() { Name = "Administrative", NameAr = "إداري", Description = "قضايا إدارية" },
            new() { Name = "Tax", NameAr = "ضريبي", Description = "قضايا ضريبية" },
            new() { Name = "Intellectual Property", NameAr = "ملكية فكرية", Description = "حقوق الملكية الفكرية" },
            new() { Name = "International", NameAr = "دولي", Description = "قضايا دولية" }
        };
    }
}