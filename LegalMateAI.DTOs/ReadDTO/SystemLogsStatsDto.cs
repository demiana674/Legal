using System;

namespace LegalMateAI.DTOs.ReadDTO
{
    /// <summary>
    /// إحصائيات سجلات النظام
    /// </summary>
    public class SystemLogsStatsDto
    {
        public int TotalLogs { get; set; }
        public int TodayLogs { get; set; }
        public int LoginAttempts { get; set; }
        public int FailedLogins { get; set; }
        public int SuccessfulLogins { get; set; }
        public int DocumentsUploaded { get; set; }
        public int CasesCreated { get; set; }
        public int AppointmentsBooked { get; set; }
        public int LawyersVerified { get; set; }
        public int LawyersRejected { get; set; }
        public int UsersRegistered { get; set; }
        public int AdminActions { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public Dictionary<string, int> ActionsByType { get; set; } = new();
    }
}