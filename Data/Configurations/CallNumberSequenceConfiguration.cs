using CallLogManagementSystem.Models.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallLogManagementSystem.Data.Configurations
{
    public class CallNumberSequenceConfiguration : IEntityTypeConfiguration<CallNumberSequence>
    {
        public void Configure(EntityTypeBuilder<CallNumberSequence> builder)
        {
            builder.HasKey(s => s.Year);

            // Year is an explicit business value we assign (e.g. 2026), not an autoincrement —
            // without this EF infers IDENTITY on an int PK and silently discards our value.
            builder.Property(s => s.Year).ValueGeneratedNever();
        }
    }
}
