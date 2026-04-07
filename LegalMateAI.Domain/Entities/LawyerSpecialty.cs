// LegalMateAI.Domain/Entities/LawyerSpecialty.cs
using System;
using System.Collections.Generic;

namespace LegalMateAI.Domain.Entities
{
    public class LawyerSpecialty
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        
        public ICollection<LawyerProfileSpecialty> LawyerProfiles { get; set; } = new List<LawyerProfileSpecialty>();
        
        // التخصصات المصرية للمحامين
        public static List<LawyerSpecialty> EgyptianLawyerSpecialties() => new()
        {
            new() { Id = 1, Name = "Personal Status", NameAr = "أحوال شخصية", Description = "قضايا الطلاق والحضانة والنفقة" },
            new() { Id = 2, Name = "Criminal", NameAr = "جنائي", Description = "قضايا جنائية ومخالفات" },
            new() { Id = 3, Name = "Civil", NameAr = "مدني", Description = "قضايا مدنية وتعويضات" },
            new() { Id = 4, Name = "Commercial", NameAr = "تجاري", Description = "قضايا تجارية وشركات" },
            new() { Id = 5, Name = "Labor", NameAr = "عمالي", Description = "قضايا عمل وعمال" },
            new() { Id = 6, Name = "Real Estate", NameAr = "عقاري", Description = "قضايا عقارية وملكية" },
            new() { Id = 7, Name = "Administrative", NameAr = "إداري", Description = "قضايا إدارية" },
            new() { Id = 8, Name = "Tax", NameAr = "ضريبي", Description = "قضايا ضريبية" },
            new() { Id = 9, Name = "Intellectual Property", NameAr = "ملكية فكرية", Description = "حقوق الملكية الفكرية" },
            new() { Id = 10, Name = "International", NameAr = "دولي", Description = "قضايا دولية" }
        };
    }
}