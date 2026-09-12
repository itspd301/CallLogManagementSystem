using CallLogManagementSystem.Models.Entities.CallManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class CallActivityConfiguration : IEntityTypeConfiguration<CallActivity>
    {
        public void Configure(EntityTypeBuilder<CallActivity> builder)
        {
            builder.HasIndex(a => a.PerformedDateTime);

            // Mirrors CallLog's soft-delete filter so a directly-queried CallActivity
            // never surfaces for a call that's been (soft-)deleted.
            builder.HasQueryFilter(a => !a.CallLog!.IsDeleted);

            builder.HasOne(a => a.PerformedBy).WithMany().HasForeignKey(a => a.PerformedById).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(a => a.OldStatus).WithMany().HasForeignKey(a => a.OldStatusId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(a => a.NewStatus).WithMany().HasForeignKey(a => a.NewStatusId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
