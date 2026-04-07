using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.UpdateDTO
{
    // 13. Update Notification Settings
    public class UpdateNotificationSettingsDto
    {
        public bool? EmailNotifications { get; set; }
        public bool? SmsNotifications { get; set; }
        public bool? PushNotifications { get; set; }
        public bool? AppointmentReminders { get; set; }
        public bool? ContractUpdates { get; set; }
        public bool? CaseUpdates { get; set; }
        public string? Language { get; set; }
    }
}








