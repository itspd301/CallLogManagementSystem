using CallLogManagementSystem.Models.Entities.Masters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.HasIndex(e => e.EmployeeCode).IsUnique();

            builder.HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Shop)
                .WithMany()
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
