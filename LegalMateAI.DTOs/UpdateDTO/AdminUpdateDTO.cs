using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class AdminUpdateDTO
    {
        [Required]
        public string Name { get; set; }= string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; }= string.Empty;

        public string? Permissions { get; set; }
        public bool IsActive { get; set; }
    }
}
