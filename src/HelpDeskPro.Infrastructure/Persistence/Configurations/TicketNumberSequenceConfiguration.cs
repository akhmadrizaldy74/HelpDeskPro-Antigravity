namespace HelpDeskPro.Infrastructure.Persistence.Configurations;

using HelpDeskPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TicketNumberSequenceConfiguration : IEntityTypeConfiguration<TicketNumberSequence>
{
    public void Configure(EntityTypeBuilder<TicketNumberSequence> builder)
    {
        builder.ToTable("TicketNumberSequences");

        builder.HasKey(s => s.Year);

        builder.Property(s => s.Year)
            .ValueGeneratedNever();

        builder.Property(s => s.LastNumber)
            .IsRequired();
    }
}

