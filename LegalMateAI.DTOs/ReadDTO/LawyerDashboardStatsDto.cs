using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class LawyerDashboardStatsDto
    {
        public int ActiveCases { get; set; }
        public int UrgentHearings { get; set; }
        public int ActiveClients { get; set; }
        public int PendingContracts { get; set; }
        public decimal PendingFees { get; set; }
        public int TodayAppointments { get; set; }
        public int NewRequests { get; set; }
        public int RescheduleRequests { get; set; }
    }
}

