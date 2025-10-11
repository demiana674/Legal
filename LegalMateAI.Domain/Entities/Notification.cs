using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.Domain.Entities
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }
        [Required]
        public int UserID { get; set; }
        [Required]
        [MaxLength(500)]
        public string Message { get; set; }= string.Empty;
        public NotificationType Type { get; set; } = NotificationType.General;
        [MaxLength(300)]
        public string? RedirectURL { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public User? User { get; set; }

    }
    public enum NotificationType
    {
        General,
        Consultation,
        Contract,
        LawUpdate,
        System
    }
}