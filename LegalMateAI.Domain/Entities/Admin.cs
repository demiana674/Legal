using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    [Index(nameof(Email), IsUnique = true)]
    public class Admin
    {
        [Key]
        public int AdminID { get; set; }
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;
        public AdminRole Role { get; set; } = AdminRole.Standard;

        public bool IsActive { get; set; }= true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }= DateTime.UtcNow;

        public ICollection<AdminLog>? AdminLogs { get; set; } = new List<AdminLog>();
        public ICollection<Consultation>? AdminConsultations { get; set; } = new List<Consultation>();
    }
    public enum AdminRole
    {
        SuperAdmin, // صلاحيات كاملة
        Standard,   // صلاحيات محدودة
        Support     // دعم فني أو مستخدم تقني
    }
}