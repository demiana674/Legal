// LegalMateAI.Domain/Enums/CaseStatus.cs
namespace LegalMateAI.Domain.Enums
{
    public enum CaseStatus
    {
        Active = 1,        // نشطة
        Pending = 2,       // قيد المراجعة
        Completed = 3,     // منتهية
        Rejected = 4,      // مرفوضة
        OnHold = 5         // معلقة
    }
}