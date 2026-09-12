using CallLogManagementSystem.Models.Entities.CallManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class CallAttachmentConfiguration : IEntityTypeConfiguration<CallAttachment>
    {
        public void Configure(EntityTypeBuilder<CallAttachment> builder)
        {
            builder.HasQueryFilter(a => !a.CallLog!.IsDeleted);

            builder.HasOne(a => a.UploadedBy).WithMany().HasForeignKey(a => a.UploadedById).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
