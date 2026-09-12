using CallLogManagementSystem.Models.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasIndex(a => a.Timestamp);
            builder.HasIndex(a => new { a.EntityName, a.EntityId });

            // Nullable UserId: some entries (failed logins with an unrecognized Employee ID)
            // have no resolvable user.
            builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
