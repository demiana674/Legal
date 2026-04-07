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
    // 11. Dashboard Response
    public class UserDashboardResponseDto
    {
        public UserBriefDto User { get; set; } = null!;
        public DashboardStatsDto Stats { get; set; } = new();
        public List<AppointmentResponseDto> UpcomingAppointments { get; set; } = new();
        public List<ContractResponseDto> RecentContracts { get; set; } = new();
        public List<DocumentResponseDto> RecentDocuments { get; set; } = new();
        public List<NotificationResponseDto> UnreadNotifications { get; set; } = new();
    }
}

