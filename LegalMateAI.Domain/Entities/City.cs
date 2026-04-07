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
         public class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // مثلاً: "مدينة نصر"
        public int GovernorateId { get; set; } // المحافظة التابعة لها
        public Governorate Governorate { get; set; } = null!;
    }
}