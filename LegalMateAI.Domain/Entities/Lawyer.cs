using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    public class Lawyer
    {
        [Key]
        public int LawyerID { get; set; }
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;
        [Phone]
        [MaxLength(20)]
        public string? Phone { get; set; }
        [MaxLength(500)]
        public string? Address { get; set; }
        [Required]
        [MaxLength(150)]
        public string Specialization { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? Description { get; set; }
        [Range(0, 80)]
        public int? ExperienceYears { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public bool IsVerified { get; set; } = false;
        public bool Status { get; set; } = true;
        [Range(0, 5)]
        public double? Rating { get; set; }


        public ICollection<Consultation>? LawyerConsultations { get; set; }= new List<Consultation>();


    }
}