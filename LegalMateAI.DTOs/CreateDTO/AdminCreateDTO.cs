using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class AdminCreateDTO
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }= string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; }= string.Empty;
        [Required, MinLength(6)]
        public string Password { get; set; }= string.Empty;
        public string? Permissions { get; set; }
        
    }
}