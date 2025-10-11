using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class ConsultationCreateDTO
    {
        [Required]
        public int UserID { get; set; }
        [Required]
        [MaxLength(2000, ErrorMessage = "السؤال لا يجب أن يتجاوز 2000 حرف.")]
        public string? Question { get; set; }

      
    }
}