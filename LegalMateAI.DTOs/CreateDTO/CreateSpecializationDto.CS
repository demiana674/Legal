using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.CreateDTO
{
    // 13. Lawyer Specialization
    public class CreateSpecializationDto
    {
        [Required(ErrorMessage = "اسم التخصص مطلوب")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsPrimary { get; set; }
    }
}