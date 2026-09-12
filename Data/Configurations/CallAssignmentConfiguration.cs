using CallLogManagementSystem.Models.Entities.CallManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class CallAssignmentConfiguration : IEntityTypeConfiguration<CallAssignment>
    {
        public void Configure(EntityTypeBuilder<CallAssignment> builder)
        {
            builder.HasQueryFilter(a => !a.CallLog!.IsDeleted);

            builder.HasOne(a => a.Engineer).WithMany().HasForeignKey(a => a.EngineerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(a => a.AssignedBy).WithMany().HasForeignKey(a => a.AssignedById).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
