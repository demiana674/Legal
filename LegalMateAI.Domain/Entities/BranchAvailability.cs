// LegalMateAI.Domain/Entities/BranchAvailability.cs
using System;

namespace LegalMateAI.Domain.Entities
{
    /// <summary>
    /// أوقات توفر المحامي في فرع معين
    /// </summary>
    public class BranchAvailability
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// معرف الفرع
        /// </summary>
        public Guid BranchId { get; set; }
        public LawyerBranch Branch { get; set; } = null!;
        
        /// <summary>
        /// اليوم (0 = الأحد، 1 = الاثنين، ...)
        /// </summary>
        public DayOfWeek DayOfWeek { get; set; }
        
        /// <summary>
        /// وقت البداية
        /// </summary>
        public TimeSpan StartTime { get; set; }
        
        /// <summary>
        /// وقت النهاية
        /// </summary>
        public TimeSpan EndTime { get; set; }
        
        /// <summary>
        /// مدة الموعد الواحد بالدقائق
        /// </summary>
        public int SlotDurationMinutes { get; set; } = 60;
        
        /// <summary>
        /// هل اليوم متاح؟
        /// </summary>
        public bool IsAvailable { get; set; } = true;
    }
}