namespace LegalMateAI.DTOs.ReadDTO
{
    public class SystemStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalLawyers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalContracts { get; set; }
        public int TotalAppointments { get; set; }
        public int PendingVerifications { get; set; }
        public int ActiveUsers { get; set; }
        public int ActiveLawyers { get; set; }
        public long TotalStorageUsed { get; set; }
        public DateTime LastBackupDate { get; set; }
        public double SystemUptime { get; set; }
    }
}