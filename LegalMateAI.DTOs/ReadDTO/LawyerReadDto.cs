using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerReadDto
    {
        public int LawyerID { get; set; }
        public string FullName { get; set; }= string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ExperienceYears { get; set; }
        public bool IsVerified { get; set; }
        public bool Status { get; set; }
        public DateTime JoinDate { get; set; }
    }
}