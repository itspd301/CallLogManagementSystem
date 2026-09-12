using CallLogManagementSystem.Models.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasIndex(n => new { n.UserId, n.IsRead });
            builder.HasQueryFilter(n => !n.CallLog!.IsDeleted);

            builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(n => n.CallLog).WithMany().HasForeignKey(n => n.CallLogId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
