using LegalMateAI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class NotificationReadDTO
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string? Message { get; set; }
        public DateTime Date { get; set; }
        public bool IsRead { get; set; }
        public NotificationType? Type { get; set; }
       
        public string? RedirectURL { get; set; }

    }
}