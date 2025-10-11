using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LegalMateAI.Domain.Entities;

namespace LegalMateAI.DAL.DBContext
{
    public class LegalMateDbContext : DbContext
    {
        public LegalMateDbContext(DbContextOptions<LegalMateDbContext> options) : base(options)
        {
        }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<AIModels> AIModels { get; set; }
        public DbSet<ModelResults> ModelResults { get; set; }
        public DbSet<Articles> Articles { get; set; }
        public DbSet<Clause> Clauses { get; set; }
        public DbSet<Law> Laws { get; set; }
        public DbSet<LawUpdates> LawUpdates { get; set; }
        public DbSet<ChatbotLog> ChatbotLogs { get; set; }
        public DbSet<Consultation> Consultations { get; set; }
        public DbSet<ContractsTemplate> ContractsTemplates { get; set; }
        public DbSet<UserContracts> UserContracts { get; set; }
        public DbSet<IRDocuments> IRDocuments { get; set; }
        public DbSet<IRQueries> IRQueries { get; set; }
        public DbSet<IRQueryDocument> IRQueryDocuments { get; set; }
        public DbSet<SearchIndex> SearchIndices { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Lawyer> Lawyers { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite primary key for IRQueryDocument
            modelBuilder.Entity<IRQueryDocument>()
                .HasKey(qd => new { qd.QueryID, qd.DocumentID });

            modelBuilder.Entity<IRQueryDocument>()
                .HasOne(qd => qd.Query)
                .WithMany(q => q.QueryDocuments)
                .HasForeignKey(qd => qd.QueryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IRQueryDocument>()
                .HasOne(qd => qd.Document)
                .WithMany(d => d.DocumentQueries)
                .HasForeignKey(qd => qd.DocumentID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}