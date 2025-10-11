using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    [Index(nameof(Status))]
    [Index(nameof(UserID))]
    public class Consultation
    {
        [Key]
        public int ConsultationID { get; set; }
        [Required]
        public int UserID { get; set; }
        
        public int? AdminID { get; set; }
        public int? LawyerID { get; set; }
        [MaxLength(300)]
        public string? Subject { get; set; }
        public ConsultationStatus Status { get; set; } = ConsultationStatus.Pending;
        [MaxLength(2000)]
        public string? Answer { get; set; }
        public DateTime DateAnswer { get; set; } = DateTime.UtcNow;
        public DateTime DateAsked { get; set; } = DateTime.UtcNow;
        [Required]
        [MaxLength(2000)]
        public string Question { get; set; }= string.Empty;
        public bool IsUrgent { get; set; } = false;


        public User User { get; set; }= null!; // Navigation property for User
        public Admin? Admin { get; set; }  // Navigation property for Admin
        public Lawyer? Lawyer { get; set; }  // Navigation property for Lawyer
    }
    public enum ConsultationStatus
    {
        Pending,    // لسه في انتظار الرد
        InProgress, // بيتم الرد حاليًا
        Answered,   // تم الرد
        Rejected,   // تم رفضها
        Closed      // انتهت رسميًا
    }
}