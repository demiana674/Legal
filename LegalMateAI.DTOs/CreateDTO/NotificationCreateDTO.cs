using LegalMateAI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.CreateDTO
{
    public class NotificationCreateDTO
    {
       
        public int UserID { get; set; }
        public string? Message { get; set; }
        public NotificationType? Type { get; set; } 
   
        public string? RedirectURL { get; set; }

    }
}