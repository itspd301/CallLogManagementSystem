using CallLogManagementSystem.Models.Entities.CallManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class CallLogConfiguration : IEntityTypeConfiguration<CallLog>
    {
        public void Configure(EntityTypeBuilder<CallLog> builder)
        {
            builder.HasIndex(c => c.CallNumber).IsUnique();

            // Section 30 — indexes for the high-traffic filter/search columns.
            builder.HasIndex(c => c.ReportedDateTime);
            builder.HasIndex(c => c.StatusId);
            builder.HasIndex(c => c.PriorityId);
            builder.HasIndex(c => c.LocationId);
            builder.HasIndex(c => c.ShopId);
            builder.HasIndex(c => c.ModuleId);
            builder.HasIndex(c => c.AttendedById);
            builder.HasIndex(c => c.ReportedById);

            builder.HasQueryFilter(c => !c.IsDeleted);

            // Master/lookup FKs: Restrict — masters are deactivated (IsActive), never deleted,
            // so there is no scenario where a cascade/set-null should fire.
            builder.HasOne(c => c.Location).WithMany().HasForeignKey(c => c.LocationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.Shop).WithMany().HasForeignKey(c => c.ShopId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.Module).WithMany().HasForeignKey(c => c.ModuleId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.ApplicationType).WithMany().HasForeignKey(c => c.ApplicationTypeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.ProblemCategory).WithMany().HasForeignKey(c => c.ProblemCategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.Problem).WithMany().HasForeignKey(c => c.ProblemId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.CallCategory).WithMany().HasForeignKey(c => c.CallCategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.Priority).WithMany().HasForeignKey(c => c.PriorityId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.Status).WithMany().HasForeignKey(c => c.StatusId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.ReportedBy).WithMany().HasForeignKey(c => c.ReportedById).OnDelete(DeleteBehavior.Restrict);

            // Engineer/User FKs: Restrict — people aren't hard-deleted either.
            builder.HasOne(c => c.AttendedBy).WithMany().HasForeignKey(c => c.AttendedById).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.HandedOverTo).WithMany().HasForeignKey(c => c.HandedOverToId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.ModifiedBy).WithMany().HasForeignKey(c => c.ModifiedById).OnDelete(DeleteBehavior.Restrict);

            // Child collections belong to the call and are removed with it.
            builder.HasMany(c => c.Activities).WithOne(a => a.CallLog).HasForeignKey(a => a.CallLogId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(c => c.Comments).WithOne(cm => cm.CallLog).HasForeignKey(cm => cm.CallLogId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(c => c.Attachments).WithOne(a => a.CallLog).HasForeignKey(a => a.CallLogId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(c => c.Handovers).WithOne(h => h.CallLog).HasForeignKey(h => h.CallLogId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(c => c.Assignments).WithOne(a => a.CallLog).HasForeignKey(a => a.CallLogId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
