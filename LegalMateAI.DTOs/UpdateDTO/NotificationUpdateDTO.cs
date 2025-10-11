using LegalMateAI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.UpdateDTO
{
    public class NotificationUpdateDTO
    {

        public bool IsRead { get; set; }
        public NotificationType? Type { get; set; } 
    
        public string? RedirectURL { get; set; }
    }
}