using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using LegalMateAI.Domain.Enums;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DAL.Configurations
{
    public static class EnumConverters
    {
        public static ValueConverter<T, string> GetEnumToStringConverter<T>() where T : struct, Enum
        {
            return new ValueConverter<T, string>(
                v => v.ToString(),
                v => (T)Enum.Parse(typeof(T), v)
            );
        }

        public static void ApplyEnumConversions(ModelBuilder modelBuilder)
        {
            // ===== User Related =====
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion(GetEnumToStringConverter<UserRole>());
           
            modelBuilder.Entity<AdminLog>()
                .Property(al => al.Action)
                .HasConversion(GetEnumToStringConverter<AdminLogAction>());    

            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasConversion(GetEnumToStringConverter<AccountStatus>());

            // ===== Lawyer Related =====
            // modelBuilder.Entity<LawyerProfile>()
            //     .Property(l => l.VerificationStatus)
            //     .HasConversion(GetEnumToStringConverter<LawyerVerificationStatus>());

            // ===== Appointment Related =====
            modelBuilder.Entity<Appointment>()
                .Property(a => a.Status)
                .HasConversion(GetEnumToStringConverter<AppointmentStatus>());

            modelBuilder.Entity<AppointmentReschedule>()
                .Property(ar => ar.InitiatedBy)
                .HasConversion(GetEnumToStringConverter<RescheduleInitiator>());

            modelBuilder.Entity<AppointmentReschedule>()
                .Property(ar => ar.Status)
                .HasConversion(GetEnumToStringConverter<RescheduleStatus>());

            // ===== Contract Related =====
            modelBuilder.Entity<Contract>()
                .Property(c => c.Type)
                .HasConversion(GetEnumToStringConverter<ContractType>());

            modelBuilder.Entity<Contract>()
                .Property(c => c.Status)
                .HasConversion(GetEnumToStringConverter<ContractStatus>());

            // ===== Document Related =====
            modelBuilder.Entity<Document>()
                .Property(d => d.DocType)
                .HasConversion(GetEnumToStringConverter<DocumentType>());

            modelBuilder.Entity<Document>()
                .Property(d => d.Status)
                .HasConversion(GetEnumToStringConverter<DocumentStatus>());

            modelBuilder.Entity<UserDocument>()
                .Property(ud => ud.DocumentType)
                .HasConversion(GetEnumToStringConverter<UserDocumentType>());

            // ===== Analysis Related =====
            modelBuilder.Entity<DocumentAnalysis>()
                .Property(da => da.Status)
                .HasConversion(GetEnumToStringConverter<AnalysisStatus>());

            modelBuilder.Entity<RiskAssessment>()
                .Property(r => r.Level)
                .HasConversion(GetEnumToStringConverter<RiskLevel>());

            modelBuilder.Entity<AdminLog>()
                .Property(al => al.Action)
                .HasConversion(GetEnumToStringConverter<AdminLogAction>());


                // ✅ تأكدي من وجود هذا السطر في Config الخاص بـ User
modelBuilder.Entity<User>()
    .Property(u => u.Status)
    .HasConversion(
        v => v.ToString(),
        v => (AccountStatus)Enum.Parse(typeof(AccountStatus), v))
    .HasMaxLength(20);
        }
    }
}