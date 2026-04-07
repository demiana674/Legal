using Microsoft.EntityFrameworkCore;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DAL.Configurations;

namespace LegalMateAI.DAL.DBContext
{
    public class LegalMateDbContext : DbContext
    {
        public LegalMateDbContext(DbContextOptions<LegalMateDbContext> options) : base(options) { }

        // ===== ✅ DbSets الأساسية (النشطة حالياً) =====
        public DbSet<User> Users { get; set; }
        public DbSet<LawyerProfile> LawyerProfiles { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<AdminProfile> AdminProfiles { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }
        public DbSet<LegalSpecialization> LegalSpecializations { get; set; }

        // ===== الجداول المؤجلة =====
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserDocument> UserDocuments { get; set; }
        public DbSet<UserSocialLink> UserSocialLinks { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<LawyerSpecialization> LawyerSpecializations { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<LawyerAvailability> LawyerAvailabilities { get; set; }
        public DbSet<LawyerReview> LawyerReviews { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentAnalysis> DocumentAnalyses { get; set; }
        public DbSet<ClauseAnalysis> ClauseAnalyses { get; set; }
        public DbSet<RiskAssessment> RiskAssessments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentReschedule> AppointmentReschedules { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractClause> ContractClauses { get; set; }
        public DbSet<ContractTemplate> ContractTemplates { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SearchQuery> SearchQueries { get; set; }
        public DbSet<EgyptianLaw> EgyptianLaws { get; set; }
        public DbSet<LawArticle> LawArticles { get; set; }
        public DbSet<ArticleClause> ArticleClauses { get; set; }
        public DbSet<ArticleVersion> ArticleVersions { get; set; }
        public DbSet<LawAmendment> LawAmendments { get; set; }
        public DbSet<LawInterpretation> LawInterpretations { get; set; }
        public DbSet<LawKeyword> LawKeywords { get; set; }
        public DbSet<CourtRuling> CourtRulings { get; set; }

        public DbSet<LawyerSpecialty> LawyerSpecialties { get; set; }
public DbSet<LawyerProfileSpecialty> LawyerProfileSpecialties { get; set; }

        // LegalMateAI.DAL/DBContext/LegalMateDbContext.cs
// أضف هذه الـ DbSets في الكلاس

public DbSet<Case> Cases { get; set; }
public DbSet<CaseDocument> CaseDocuments { get; set; }
public DbSet<CaseNote> CaseNotes { get; set; }
        // public DbSet<LawyerSpecialization> LawyerSpecializations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== 1. Enums as string =====
            EnumConverters.ApplyEnumConversions(modelBuilder);

            // ===== 2. منع Identity للجداول اللي بنحط Id بنفسنا =====
            
            // المحافظات والمدن
            modelBuilder.Entity<Governorate>()
                .Property(g => g.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<City>()
                .Property(c => c.Id)
                .ValueGeneratedNever();

            // القوانين والمواد والتعديلات
            modelBuilder.Entity<EgyptianLaw>()
                .Property(el => el.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<LawArticle>()
                .Property(la => la.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<LawAmendment>()
                .Property(la => la.Id)
                .ValueGeneratedNever();
                
// في OnModelCreating
// Appointment relationships
modelBuilder.Entity<Appointment>()
    .HasOne(a => a.User)
    .WithMany(u => u.Appointments)
    .HasForeignKey(a => a.UserID)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Appointment>()
    .HasOne(a => a.Lawyer)
    .WithMany(l => l.Appointments)
    .HasForeignKey(a => a.LawyerId)
    .OnDelete(DeleteBehavior.Restrict);

            // ===== 3. Relationships for Active Tables =====

            // Admin & AdminProfile
            modelBuilder.Entity<Admin>()
                .HasOne(a => a.Profile)
                .WithOne(ap => ap.Admin)
                .HasForeignKey<AdminProfile>(ap => ap.AdminId)
                .OnDelete(DeleteBehavior.Cascade);

            // Admin & AdminLogs
            modelBuilder.Entity<Admin>()
                .HasMany(a => a.AdminLogs)
                .WithOne(al => al.Admin)
                .HasForeignKey(al => al.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // User & LawyerProfile
            modelBuilder.Entity<User>()
                .HasOne(u => u.LawyerProfile)
                .WithOne(l => l.User)
                .HasForeignKey<LawyerProfile>(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Governorate & City
            modelBuilder.Entity<Governorate>()
                .HasMany(g => g.Cities)
                .WithOne(c => c.Governorate)
                .HasForeignKey(c => c.GovernorateId)
                .OnDelete(DeleteBehavior.Cascade);



                // في LegalMateDbContext.cs - داخل OnModelCreating

// ===== Lawyer Profile Specialties =====
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

modelBuilder.Entity<LawyerProfileSpecialty>()
    .HasIndex(lps => new { lps.LawyerId, lps.SpecialtyId })
    .IsUnique();

            // LawyerProfile & Governorate
            modelBuilder.Entity<LawyerProfile>()
                .HasOne(l => l.Governorate)
                .WithMany(g => g.Lawyers)
                .HasForeignKey(l => l.GovernorateId)
                .OnDelete(DeleteBehavior.SetNull);

            // LoginAttempt & User
            modelBuilder.Entity<LoginAttempt>()
                .HasOne(la => la.User)
                .WithMany()
                .HasForeignKey(la => la.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // DocumentAnalysis (Multiple cascade paths)
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



           // ✅ العلاقة بين UserProfile و Governorate
    modelBuilder.Entity<UserProfile>()
        .HasOne(up => up.Governorate)
        .WithMany()
        .HasForeignKey(up => up.GovernorateId)
        .OnDelete(DeleteBehavior.NoAction);

    // ✅ العلاقة بين UserProfile و City
    modelBuilder.Entity<UserProfile>()
        .HasOne(up => up.City)
        .WithMany()
        .HasForeignKey(up => up.CityId)
        .OnDelete(DeleteBehavior.NoAction);

    // ✅ العلاقة بين UserProfile و User
    modelBuilder.Entity<UserProfile>()
        .HasOne(up => up.User)
        .WithOne(u => u.UserProfile)
        .HasForeignKey<UserProfile>(up => up.UserId)
        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentAnalysis>()
                .HasMany(da => da.Risks)
                .WithOne(r => r.Analysis)
                .HasForeignKey(r => r.AnalysisId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointment (Multiple cascade paths)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Lawyer)
                .WithMany()
                .HasForeignKey(a => a.LawyerId)
                .OnDelete(DeleteBehavior.Cascade);

            // LawyerReview
            modelBuilder.Entity<LawyerReview>()
                .HasOne(lr => lr.User)
                .WithMany()
                .HasForeignKey(lr => lr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== 4. Indexes =====
            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Role);

            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Email)
                .IsUnique();

            modelBuilder.Entity<LawyerProfile>()
                .HasIndex(l => l.VerificationStatus);

            modelBuilder.Entity<Governorate>()
                .HasIndex(g => g.Name);


                // ✅ Index لتسريع البحث بالبريد الإلكتروني
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique();
    
    // ✅ Index لتسريع البحث بالرقم القومي ومنع التكرار
    modelBuilder.Entity<User>()
        .HasIndex(u => u.NationalId)
        .IsUnique();
    
    // ✅ Index لتسريع البحث برقم رخصة المحاماة ومنع التكرار
    modelBuilder.Entity<LawyerProfile>()
        .HasIndex(l => l.LicenseNumber)
        .IsUnique();
    
    // Index للحالة
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Role);
    
    modelBuilder.Entity<LawyerProfile>()
        .HasIndex(l => l.VerificationStatus);

            // ===== 5. Default Values =====
            
            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasDefaultValue(AccountStatus.Pending);

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<User>()
                .Property(u => u.JoinDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Admin>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<AdminLog>()
                .Property(al => al.Timestamp)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<LawyerProfile>()
                .Property(l => l.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<LawyerProfile>()
                .Property(l => l.VerificationStatus)
                .HasDefaultValue(LawyerVerificationStatus.Pending);

            modelBuilder.Entity<LoginAttempt>()
                .Property(la => la.AttemptedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}