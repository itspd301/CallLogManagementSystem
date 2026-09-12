using CallLogManagementSystem.Models.Entities.CallManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class CallCommentConfiguration : IEntityTypeConfiguration<CallComment>
    {
        public void Configure(EntityTypeBuilder<CallComment> builder)
        {
            builder.HasQueryFilter(c => !c.CallLog!.IsDeleted);

            builder.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
