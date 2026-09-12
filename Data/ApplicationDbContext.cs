using CallLogManagementSystem.Models.Entities.CallManagement;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Models.Entities.Masters;
using CallLogManagementSystem.Models.Entities.System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Masters
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<Shop> Shops => Set<Shop>();
        public DbSet<Module> Modules => Set<Module>();
        public DbSet<ApplicationType> ApplicationTypes => Set<ApplicationType>();
        public DbSet<CallCategory> CallCategories => Set<CallCategory>();
        public DbSet<ProblemCategory> ProblemCategories => Set<ProblemCategory>();
        public DbSet<Problem> Problems => Set<Problem>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Engineer> Engineers => Set<Engineer>();
        public DbSet<Status> Statuses => Set<Status>();
        public DbSet<Priority> Priorities => Set<Priority>();
        public DbSet<SLAConfiguration> SLAConfigurations => Set<SLAConfiguration>();

        // Call Management
        public DbSet<CallLog> CallLogs => Set<CallLog>();
        public DbSet<CallActivity> CallActivities => Set<CallActivity>();
        public DbSet<CallComment> CallComments => Set<CallComment>();
        public DbSet<CallAttachment> CallAttachments => Set<CallAttachment>();
        public DbSet<CallHandover> CallHandovers => Set<CallHandover>();
        public DbSet<CallAssignment> CallAssignments => Set<CallAssignment>();

        // System
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<CallNumberSequence> CallNumberSequences => Set<CallNumberSequence>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.EmployeeId).HasMaxLength(20).IsRequired();
                entity.Property(u => u.FullName).HasMaxLength(150).IsRequired();
                entity.Property(u => u.Designation).HasMaxLength(100);
                entity.HasIndex(u => u.EmployeeId).IsUnique();
            });

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
