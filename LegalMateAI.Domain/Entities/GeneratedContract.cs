// LegalMateAI.Domain/Entities/GeneratedContract.cs
using System;
using LegalMateAI.Domain.Enums;

namespace LegalMateAI.Domain.Entities
{
    /// <summary>
    /// عقد تم إنشاؤه من قالب جاهز
    /// يحفظ مسار الملف النهائي بعد تعبئة البيانات
    /// </summary>
    public class GeneratedContract
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// رقم العقد الفريد
        /// </summary>
        public string ContractNumber { get; set; } = string.Empty;
        
        /// <summary>
        /// عنوان العقد
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// القالب المستخدم
        /// </summary>
        public Guid TemplateId { get; set; }
        public PredefinedContractTemplate Template { get; set; } = null!;
        
        /// <summary>
        /// المستخدم الذي أنشأ العقد
        /// </summary>
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        /// <summary>
        /// المحامي المرتبط (اختياري)
        /// </summary>
        public Guid? LawyerId { get; set; }
        public LawyerProfile? Lawyer { get; set; }
        
        /// <summary>
        /// البيانات المدخلة من المستخدم (JSON)
        /// مثال: {"FullName": "أحمد محمد", "NationalId": "12345678901234", ...}
        /// </summary>
        public string FilledDataJson { get; set; } = "{}";
        
        /// <summary>
        /// مسار ملف PDF النهائي بعد تعبئة البيانات
        /// </summary>
        public string FinalPdfPath { get; set; } = string.Empty;
        
        /// <summary>
        /// حالة العقد
        /// </summary>
        public ContractStatus Status { get; set; } = ContractStatus.Draft;
        
        /// <summary>
        /// تاريخ الإنشاء
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// تاريخ انتهاء صلاحية الملف (اختياري - لحذف الملفات القديمة)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}