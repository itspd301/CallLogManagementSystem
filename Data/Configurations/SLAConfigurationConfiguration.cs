using CallLogManagementSystem.Models.Entities.Masters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class SLAConfigurationConfiguration : IEntityTypeConfiguration<SLAConfiguration>
    {
        public void Configure(EntityTypeBuilder<SLAConfiguration> builder)
        {
            builder.HasOne(s => s.Priority)
                .WithMany(p => p.SLAConfigurations)
                .HasForeignKey(s => s.PriorityId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
