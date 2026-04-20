// LegalMateAI.Domain/Entities/LawyerBranch.cs
using System;
using System.Collections.Generic;

namespace LegalMateAI.Domain.Entities
{
    /// <summary>
    /// فروع مكتب المحامي
    /// </summary>
    public class LawyerBranch
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// معرف المحامي
        /// </summary>
        public Guid LawyerId { get; set; }
        public LawyerProfile Lawyer { get; set; } = null!;
        
        /// <summary>
        /// اسم الفرع
        /// </summary>
        public string BranchName { get; set; } = string.Empty;
        
        /// <summary>
        /// المحافظة
        /// </summary>
        public int? GovernorateId { get; set; }
        public Governorate? Governorate { get; set; }
        
        /// <summary>
        /// المدينة
        /// </summary>
        public string? City { get; set; }
        
        /// <summary>
        /// العنوان التفصيلي
        /// </summary>
        public string Address { get; set; } = string.Empty;
        
        /// <summary>
        /// رقم الهاتف
        /// </summary>
        public string? PhoneNumber { get; set; }
        
        /// <summary>
        /// هل الفرع نشط؟
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// تاريخ الإنشاء
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// أوقات التوفر في هذا الفرع
        /// </summary>
        public ICollection<BranchAvailability> Availabilities { get; set; } = new List<BranchAvailability>();
        
        /// <summary>
        /// المواعيد المحجوزة في هذا الفرع
        /// </summary>
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}