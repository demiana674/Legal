using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class LawyerUpdateDTO

    {
        [Phone]
        public string? Phone { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(0, 60)]
        public int? ExperienceYears { get; set; }

        public bool IsVerified { get; set; }
        public bool Status { get; set; }
    }
}