using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    public class ChatbotLog
    {
        [Key]
        public int ChatID { get; set; }
        [Required]
        public int UserID { get; set; }
        public string? SessionID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        public string Message { get; set; } = string.Empty;
        public string? Response { get; set; }
        public User User { get; set; }= null!;
    }
}