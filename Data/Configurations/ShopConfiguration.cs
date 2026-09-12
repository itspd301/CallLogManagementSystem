using CallLogManagementSystem.Models.Entities.Masters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class ShopConfiguration : IEntityTypeConfiguration<Shop>
    {
        public void Configure(EntityTypeBuilder<Shop> builder)
        {
            builder.HasIndex(s => new { s.LocationId, s.Code }).IsUnique();

            builder.HasOne(s => s.Location)
                .WithMany(l => l.Shops)
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
