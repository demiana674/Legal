using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AdminLogReadDTO
    {
        public int LogID { get; set; }
        public int AdminID { get; set; }
        public string? AdminName { get; set; } 
        public string? ActionType { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
        public string? EntityName { get; set; }
        public int? EntityID { get; set; }
    }
}
