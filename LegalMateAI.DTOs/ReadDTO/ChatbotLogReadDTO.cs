using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class ChatbotLogReadDTO
    {
        public int ChatID { get; set; }
        public int UserID { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Message { get; set; }
        public string? Response { get; set; }
    }
}