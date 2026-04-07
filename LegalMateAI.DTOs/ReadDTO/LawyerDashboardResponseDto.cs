using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerDashboardResponseDto
    {
        public LawyerBriefDto Lawyer { get; set; } = null!;
        public LawyerDashboardStatsDto Stats { get; set; } = new();
        public List<AppointmentResponseDto> TodayAppointments { get; set; } = new();
        public List<AppointmentResponseDto> PendingRequests { get; set; } = new();
        public List<CaseBriefDto> ActiveCases { get; set; } = new();
        public List<ClientBriefDto> RecentClients { get; set; } = new();
        public List<ActivityDto> RecentActivity { get; set; } = new();
    }
}

