using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class ChatbotLogCreateDTO
    {
        [Required]
        public int UserID { get; set; }
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
        public string? Response { get; set; }

    }
}