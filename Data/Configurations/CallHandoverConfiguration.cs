using CallLogManagementSystem.Models.Entities.CallManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class CallHandoverConfiguration : IEntityTypeConfiguration<CallHandover>
    {
        public void Configure(EntityTypeBuilder<CallHandover> builder)
        {
            builder.HasQueryFilter(h => !h.CallLog!.IsDeleted);

            builder.HasOne(h => h.FromEngineer).WithMany().HasForeignKey(h => h.FromEngineerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(h => h.ToEngineer).WithMany().HasForeignKey(h => h.ToEngineerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(h => h.PerformedBy).WithMany().HasForeignKey(h => h.PerformedById).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
