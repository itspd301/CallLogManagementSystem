using CallLogManagementSystem.Models.Entities.Masters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class ProblemConfiguration : IEntityTypeConfiguration<Problem>
    {
        public void Configure(EntityTypeBuilder<Problem> builder)
        {
            builder.HasOne(p => p.ProblemCategory)
                .WithMany(pc => pc.Problems)
                .HasForeignKey(p => p.ProblemCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Module)
                .WithMany()
                .HasForeignKey(p => p.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
