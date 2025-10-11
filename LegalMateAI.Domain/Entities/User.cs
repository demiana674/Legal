using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LegalMateAI.Domain.Entities
{
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key]
        public int UserID { get; set; }
        [Required]
        [MaxLength(150)]

        public string Name { get; set;}= string.Empty;
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; }= string.Empty;
        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }= true;
        public UserRole Role { get; set; }= UserRole.User;

        public ICollection<UserContracts> UserContracts { get; set; }= new List<UserContracts>();
        public ICollection<Consultation> UserConsultations { get; set; }= new List<Consultation>();
        public ICollection<ChatbotLog> ChatbotLogs { get; set; } = new List<ChatbotLog>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<IRDocuments> IRDocuments { get; set; } = new List<IRDocuments>();
        public ICollection<IRQueries> IRQueries { get; set; } = new List<IRQueries>();

    }
    public enum UserRole
    {
        Admin,
        User,
        Lawyer
    }

}