using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using LegalMateAI.Domain.Enums;
namespace LegalMateAI.DTOs.CreateDTO
{
    // 15. Notification
    public class CreateNotificationDto
    {
        [Required(ErrorMessage = "معرف المستخدم مطلوب")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "عنوان الإشعار مطلوب")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "محتوى الإشعار مطلوب")]
        public string Content { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.Info;

        public string? ActionUrl { get; set; }
    }
}



