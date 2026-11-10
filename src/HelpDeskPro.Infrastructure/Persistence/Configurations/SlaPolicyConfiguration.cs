namespace HelpDeskPro.Infrastructure.Persistence.Configurations;

using HelpDeskPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("SlaPolicies");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Priority)
            .HasConversion<byte>()
            .IsRequired();

        builder.HasIndex(s => s.Priority)
            .IsUnique();

        builder.Property(s => s.ResponseTimeMinutes)
            .IsRequired();

        builder.Property(s => s.ResolutionTimeMinutes)
            .IsRequired();
    }
}

