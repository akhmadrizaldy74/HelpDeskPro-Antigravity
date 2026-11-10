namespace HelpDeskPro.Infrastructure.Persistence.Configurations;

using HelpDeskPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EscalationConfiguration : IEntityTypeConfiguration<Escalation>
{
    public void Configure(EntityTypeBuilder<Escalation> builder)
    {
        builder.ToTable("Escalations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Level)
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(e => e.Note)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.HasOne(e => e.Ticket)
            .WithMany(t => t.Escalations)
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EscalatedTo)
            .WithMany(u => u.EscalationsReceived)
            .HasForeignKey(e => e.EscalatedToId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EscalatedBy)
            .WithMany(u => u.EscalationsCreated)
            .HasForeignKey(e => e.EscalatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TicketId, e.Level, e.Reason })
            .IsUnique()
            .HasFilter("[Reason] <> 3");

        builder.HasQueryFilter(e => !e.Ticket.IsDeleted);
    }
}

