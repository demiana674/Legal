using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class AdminReadDTO
    {
        public int AdminID { get; set; }
        public string Name { get; set; }= string.Empty;
        public string Email { get; set; }= string.Empty;

        public string? Permissions { get; set; }
        public bool IsActive { get; set; }
    }
}