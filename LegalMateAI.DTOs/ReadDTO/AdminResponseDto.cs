using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LegalMateAI.DTOs.ReadDTO
{
    // 3. استجابة أدمن
    public class AdminResponseDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        // public AdminRole Role { get; set; }
        // public string RoleName => Role switch
        // {
        //     AdminRole.SuperAdmin => "مدير عام",
        //     AdminRole.ContentManager => "مدير محتوى",
        //     AdminRole.LawyerVerifier => "مدقق محامين",
        //     AdminRole.UserManager => "مدير مستخدمين",
        //     AdminRole.AnalyticsViewer => "مشاهد تقارير",
        //     AdminRole.SupportAgent => "دعم فني",
        //     _ => Role.ToString()
        // };
        // public AdminStatus Status { get; set; }
        // public string StatusName => Status switch
        // {
        //     AdminStatus.Active => "نشط",
        //     AdminStatus.Inactive => "غير نشط",
        //     AdminStatus.Suspended => "موقوف",
        //     AdminStatus.PendingVerification => "بإنتظار التحقق",
        //     _ => Status.ToString()
        // };
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int LogsCount { get; set; }
    }
}

