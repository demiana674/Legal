using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.UpdateDTO
{
    // 11. Update Specialization
    public class UpdateSpecializationDto
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public bool? IsPrimary { get; set; }

        public int? CasesCount { get; set; }
    }
}
