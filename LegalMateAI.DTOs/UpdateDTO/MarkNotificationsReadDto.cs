using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.UpdateDTO
{
    // 14. Mark Notifications Read
    public class MarkNotificationsReadDto
    {
        public Guid[]? NotificationIds { get; set; }
        public bool MarkAll { get; set; }
    }
}