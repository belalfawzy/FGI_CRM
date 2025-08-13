using FGI.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FGI.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<LeadFeedback> LeadFeedbacks { get; set; }
        public DbSet<LeadAssignmentHistory> LeadAssignmentHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enum Conversions
            modelBuilder.Entity<Lead>()
                .Property(e => e.CurrentStatus)
                .HasConversion(new EnumToStringConverter<LeadStatusType>());

            modelBuilder.Entity<LeadFeedback>()
                .Property(f => f.Status)
                .HasConversion(new EnumToStringConverter<LeadStatusType>());

            // Lead ↔ User
            modelBuilder.Entity<Lead>()
                .HasOne(l => l.CreatedBy)
                .WithMany(u => u.CreatedLeads)
                .HasForeignKey(l => l.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lead>()
                .HasOne(l => l.AssignedTo)
                .WithMany(u => u.AssignedLeads)
                .HasForeignKey(l => l.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            // Lead ↔ Project
            modelBuilder.Entity<Lead>()
                .HasOne(l => l.Project)
                .WithMany(p => p.Leads)
                .HasForeignKey(l => l.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Lead ↔ Unit
            modelBuilder.Entity<Lead>()
                .HasOne(l => l.Unit)
                .WithMany()
                .HasForeignKey(l => l.UnitId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unit ↔ Project
            modelBuilder.Entity<Unit>()
                .HasOne(u => u.Project)
                .WithMany(p => p.Units)
                .HasForeignKey(u => u.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // LeadFeedback ↔ Lead
            modelBuilder.Entity<LeadFeedback>()
                .HasOne(f => f.Lead)
                .WithMany(l => l.Feedbacks)
                .HasForeignKey(f => f.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            // LeadFeedback ↔ User (Sales)
            modelBuilder.Entity<LeadFeedback>()
                .HasOne(f => f.Sales)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(f => f.SalesId)
                .OnDelete(DeleteBehavior.Restrict);

            // LeadAssignmentHistory ↔ Lead
            modelBuilder.Entity<LeadAssignmentHistory>()
                .HasOne(h => h.Lead)
                .WithMany(l => l.AssignmentHistory)
                .HasForeignKey(h => h.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            // LeadAssignmentHistory ↔ Users
            modelBuilder.Entity<LeadAssignmentHistory>()
                .HasOne(h => h.FromSales)
                .WithMany()
                .HasForeignKey(h => h.FromSalesId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeadAssignmentHistory>()
                .HasOne(h => h.ToSales)
                .WithMany()
                .HasForeignKey(h => h.ToSalesId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeadAssignmentHistory>()
                .HasOne(h => h.ChangedBy)
                .WithMany(u => u.ChangesMade)
                .HasForeignKey(h => h.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
