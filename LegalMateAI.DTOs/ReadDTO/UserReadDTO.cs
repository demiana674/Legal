using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class UserReadDTO
    {
        public int UserID { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
       
        public DateTime JoinDate { get; set; }
        public bool IsActive { get; set; }
        
        public UserRole Role { get; set; }
    }


}