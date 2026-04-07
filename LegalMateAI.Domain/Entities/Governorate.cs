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
      // 1. المحافظات
    public class Governorate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // مثلاً: "القاهرة"
        
        // العلاقة: المحافظة فيها كذا مدينة
        public ICollection<City> Cities { get; set; } = new List<City>();
         public ICollection<UserProfile> Users { get; set; } = new List<UserProfile>();
        public ICollection<LawyerProfile> Lawyers { get; set; } = new List<LawyerProfile>();
    }
}