using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
namespace LegalMateAI.DTOs.UpdateDTO
{
    // 1. Update User Profile
    public class UpdateUserDto
    {
        [StringLength(50, MinimumLength = 2)]
        public string? FirstName { get; set; }

        [StringLength(50, MinimumLength = 2)]
        public string? LastName { get; set; }
          public string? Phone { get; set; }
        public string? City { get; set; }
        public string? NationalId { get; set; }

        public string? Address { get; set; }

        public string? Gender { get; set; }

        public string? Nationality { get; set; }
    }
}


