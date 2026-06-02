using Microsoft.EntityFrameworkCore;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DAL.Configurations;

namespace LegalMateAI.DAL.DBContext
{
    public class LegalMateDbContext : DbContext
    {
        public LegalMateDbContext(DbContextOptions<LegalMateDbContext> options) : base(options) { }

        // ===== DbSets الأساسية =====
        public DbSet<User> Users { get; set; }
        public DbSet<LawyerProfile> LawyerProfiles { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<AdminProfile> AdminProfiles { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }
        public DbSet<LegalSpecialization> LegalSpecializations { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserDocument> UserDocuments { get; set; }
        public DbSet<LawyerSpecialization> LawyerSpecializations { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<LawyerAvailability> LawyerAvailabilities { get; set; }
        public DbSet<LawyerReview> LawyerReviews { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<AppointmentCancelRequest> AppointmentCancelRequests { get; set; }
        public DbSet<DocumentAnalysis> DocumentAnalyses { get; set; }
        public DbSet<ClauseAnalysis> ClauseAnalyses { get; set; }
        public DbSet<RiskAssessment> RiskAssessments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentReschedule> AppointmentReschedules { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractTemplate> ContractTemplates { get; set; }
        public DbSet<SearchQuery> SearchQueries { get; set; }
        public DbSet<Law> Laws { get; set; }
        public DbSet<LawyerBranch> LawyerBranches { get; set; }
        public DbSet<BranchAvailability> BranchAvailabilities { get; set; }
        public DbSet<LawyerSpecialty> LawyerSpecialties { get; set; }
        public DbSet<LawyerProfileSpecialty> LawyerProfileSpecialties { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<CaseDocument> CaseDocuments { get; set; }
        public DbSet<CaseNote> CaseNotes { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<PredefinedContractTemplate> PredefinedContractTemplates { get; set; }
        public DbSet<GeneratedContract> GeneratedContracts { get; set; }

         

        // ===== إضافات Data Warehouse & Analytics =====
        public DbSet<DataWarehouseFact> DataWarehouseFacts { get; set; }
        public DbSet<TimeDimension> TimeDimensions { get; set; }
        public DbSet<UserDimension> UserDimensions { get; set; }
        public DbSet<CaseDimension> CaseDimensions { get; set; }
        public DbSet<LocationDimension> LocationDimensions { get; set; }
        public DbSet<RecommendationLog> RecommendationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== 1. Enums as string =====
            EnumConverters.ApplyEnumConversions(modelBuilder);

            // ===== 2. منع Identity =====
            modelBuilder.Entity<Governorate>().Property(g => g.Id).ValueGeneratedNever();
            modelBuilder.Entity<City>().Property(c => c.Id).ValueGeneratedNever();

            // ===== 3. TimeDimension Identity off =====
            modelBuilder.Entity<TimeDimension>().Property(t => t.Id).ValueGeneratedNever();

            // ===== 4. Relationships =====

            modelBuilder.Entity<Admin>()
                .HasOne(a => a.Profile)
                .WithOne(ap => ap.Admin)
                .HasForeignKey<AdminProfile>(ap => ap.AdminId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.LawyerProfile)
                .WithOne(l => l.User)
                .HasForeignKey<LawyerProfile>(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Governorate>()
                .HasMany(g => g.Cities)
                .WithOne(c => c.Governorate)
                .HasForeignKey(c => c.GovernorateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LawyerProfileSpecialty>()
                .HasOne(lps => lps.Lawyer)
                .WithMany(lp => lp.Specialties)
                .HasForeignKey(lps => lps.LawyerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LawyerProfileSpecialty>()
                .HasOne(lps => lps.Specialty)
                .WithMany(ls => ls.LawyerProfiles)
                .HasForeignKey(lps => lps.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<AppointmentCancelRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Appointment)
                    .WithMany(a => a.CancelRequests)
                    .HasForeignKey(e => e.AppointmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });    

            modelBuilder.Entity<LegalSpecialization>()
                .Property(l => l.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Law>()
                .HasOne(l => l.AddedByAdmin)
                .WithMany()
                .HasForeignKey(l => l.AddedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LawyerBranch>()
                .HasOne(b => b.Lawyer)
                .WithMany()
                .HasForeignKey(b => b.LawyerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LawyerBranch>()
                .HasMany(b => b.Availabilities)
                .WithOne(a => a.Branch)
                .HasForeignKey(a => a.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LawyerProfile>()
                .HasOne(l => l.Governorate)
                .WithMany(g => g.Lawyers)
                .HasForeignKey(l => l.GovernorateId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LawyerProfile>()
                .HasOne(l => l.City)
                .WithMany()
                .HasForeignKey(l => l.CityId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LoginAttempt>()
                .HasOne(la => la.User)
                .WithMany()
                .HasForeignKey(la => la.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LoginAttempt>()
                .HasOne(la => la.Admin)
                .WithMany()
                .HasForeignKey(la => la.AdminId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DocumentAnalysis>()
                .HasOne(da => da.Document)
                .WithMany(d => d.Analyses)
                .HasForeignKey(da => da.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentAnalysis>()
                .HasOne(da => da.User)
                .WithMany()
                .HasForeignKey(da => da.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentAnalysis>()
                .HasMany(da => da.Clauses)
                .WithOne(c => c.Analysis)
                .HasForeignKey(c => c.AnalysisId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentAnalysis>()
                .HasMany(da => da.Risks)
                .WithOne(r => r.Analysis)
                .HasForeignKey(r => r.AnalysisId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== UserProfile Relationships =====
            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.City)
                .WithMany()
                .HasForeignKey(up => up.CityId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.User)
                .WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Appointment Relationships =====
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Lawyer)
                .WithMany()
                .HasForeignKey(a => a.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Branch)
                .WithMany(b => b.Appointments)
                .HasForeignKey(a => a.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LawyerReview>()
                .HasOne(lr => lr.User)
                .WithMany()
                .HasForeignKey(lr => lr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PredefinedContractTemplate>()
                .HasOne(t => t.CreatedByAdmin)
                .WithMany()
                .HasForeignKey(t => t.CreatedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PredefinedContractTemplate>()
                .HasMany(t => t.GeneratedContracts)
                .WithOne(g => g.Template)
                .HasForeignKey(g => g.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GeneratedContract>()
                .HasOne(g => g.User)
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GeneratedContract>()
                .HasOne(g => g.Lawyer)
                .WithMany()
                .HasForeignKey(g => g.LawyerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminProfile>()
                .HasOne(ap => ap.Governorate)
                .WithMany()
                .HasForeignKey(ap => ap.GovernorateId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AdminProfile>()
                .HasOne(ap => ap.City)
                .WithMany()
                .HasForeignKey(ap => ap.CityId)
                .OnDelete(DeleteBehavior.NoAction);

            // ===== Data Warehouse Relationships =====
            modelBuilder.Entity<DataWarehouseFact>()
                .HasOne(f => f.TimeDim)
                .WithMany()
                .HasForeignKey(f => f.TimeDimId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DataWarehouseFact>()
                .HasOne(f => f.UserDim)
                .WithMany()
                .HasForeignKey(f => f.UserDimId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DataWarehouseFact>()
                .HasOne(f => f.CaseDim)
                .WithMany()
                .HasForeignKey(f => f.CaseDimId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DataWarehouseFact>()
                .HasOne(f => f.LocationDim)
                .WithMany()
                .HasForeignKey(f => f.LocationDimId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Case Relationships =====
            modelBuilder.Entity<Case>()
                .HasOne(c => c.Client)
                .WithMany()
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Case>()
                .HasOne(c => c.Lawyer)
                .WithMany()
                .HasForeignKey(c => c.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CaseDocument>()
                .HasOne(cd => cd.Case)
                .WithMany(c => c.Documents)
                .HasForeignKey(cd => cd.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CaseNote>()
                .HasOne(cn => cn.Case)
                .WithMany(c => c.Notes)
                .HasForeignKey(cn => cn.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Indexes =====
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Role);
            modelBuilder.Entity<User>().HasIndex(u => u.NationalId).IsUnique();

            modelBuilder.Entity<Admin>().HasIndex(a => a.Email).IsUnique();

            // ❌ تم حذف هذا السطر: modelBuilder.Entity<LawyerProfile>().HasIndex(l => l.VerificationStatus);
            modelBuilder.Entity<LawyerProfile>().HasIndex(l => l.LicenseNumber).IsUnique();

            modelBuilder.Entity<Governorate>().HasIndex(g => g.Name);// ✅ تخصصات المحامي
            modelBuilder.Entity<GeneratedContract>().HasIndex(g => g.ContractNumber).IsUnique();
            modelBuilder.Entity<GeneratedContract>().HasIndex(g => g.UserId);
            modelBuilder.Entity<GeneratedContract>().HasIndex(g => g.CreatedAt);

            modelBuilder.Entity<PredefinedContractTemplate>().HasIndex(t => t.ContractType);
            modelBuilder.Entity<PredefinedContractTemplate>().HasIndex(t => t.IsActive);

            modelBuilder.Entity<Law>().HasIndex(l => l.Category);
            modelBuilder.Entity<Law>().HasIndex(l => l.IsActive);

            modelBuilder.Entity<BranchAvailability>()
                .HasIndex(a => new { a.BranchId, a.DayOfWeek });

            // ===== Data Warehouse Indexes =====
            modelBuilder.Entity<DataWarehouseFact>()
                .HasIndex(f => new { f.TimeDimId, f.CaseDimId, f.LocationDimId });

            modelBuilder.Entity<RecommendationLog>()
                .HasIndex(r => r.UserId);
            
            modelBuilder.Entity<RecommendationLog>()
                .HasIndex(r => r.CreatedAt);

            // ✅ إضافة Index على Status في جدول Users (لتحسين أداء البحث)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Status)
                .HasDatabaseName("IX_Users_Status");

            // ✅ أيضاً يمكن إضافة Index مركب (Composite Index) للبحث المتقدم
            modelBuilder.Entity<User>()
                .HasIndex(u => new { u.Role, u.Status })
                .HasDatabaseName("IX_Users_Role_Status");

            // ===== Default Values =====
            // ✅ فقط أعمدة التخزين الفعلية (وليس الخصائص المحسوبة)
            modelBuilder.Entity<User>().Property(u => u.Status).HasDefaultValue(AccountStatus.Pending);
            // ❌ تم حذف: modelBuilder.Entity<User>().Property(u => u.IsActive).HasDefaultValue(false);
            modelBuilder.Entity<User>().Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<User>().Property(u => u.JoinDate).HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Admin>().Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<AdminLog>().Property(al => al.Timestamp).HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<LawyerProfile>().Property(l => l.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            // ❌ تم حذف: modelBuilder.Entity<LawyerProfile>().Property(l => l.VerificationStatus).HasDefaultValue(LawyerVerificationStatus.Pending);

            modelBuilder.Entity<LoginAttempt>().Property(la => la.AttemptedAt).HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<PredefinedContractTemplate>().Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<PredefinedContractTemplate>().Property(t => t.IsActive).HasDefaultValue(true);

            modelBuilder.Entity<GeneratedContract>().Property(g => g.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<GeneratedContract>().Property(g => g.Status).HasDefaultValue(ContractStatus.Draft);

            modelBuilder.Entity<DataWarehouseFact>().Property(f => f.RecordedAt).HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<RecommendationLog>().Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}