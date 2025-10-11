using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class AdminLogCreateDTO
    {
        public int AdminID { get; set; }
        public string? ActionType { get; set; }
        
        public string? Details { get; set; }
        public string? EntityName { get; set; }
        public int? EntityID { get; set; }
    }
}