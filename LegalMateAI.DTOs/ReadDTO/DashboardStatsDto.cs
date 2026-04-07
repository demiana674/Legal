using System;
using LegalMateAI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegalMateAI.DTOs.ReadDTO
{
    public class DashboardStatsDto
    {
        public int TotalContracts { get; set; }
        public int ActiveContracts { get; set; }
        public int PendingContracts { get; set; }
        public int ExpiredContracts { get; set; }
        public int TotalAppointments { get; set; }
        public int ConfirmedAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int TotalDocuments { get; set; }
        public int VerifiedDocuments { get; set; }
        public DateTime? NextAppointment { get; set; }
    }
}
