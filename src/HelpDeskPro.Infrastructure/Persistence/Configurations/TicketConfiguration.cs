namespace HelpDeskPro.Infrastructure.Persistence.Configurations;

using HelpDeskPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketNo)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(t => t.TicketNo)
            .IsUnique();

        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(t => t.Priority)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(t => t.TotalPausedMinutes)
            .HasDefaultValue(0);

        builder.Property(t => t.IsSlaBreached)
            .HasDefaultValue(false);

        builder.Property(t => t.EscalationLevel)
            .HasDefaultValue((byte)0);

        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100);

        builder.Property(t => t.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Tickets)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Requester)
            .WithMany(u => u.RequestedTickets)
            .HasForeignKey(t => t.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.SlaPolicy)
            .WithMany(p => p.Tickets)
            .HasForeignKey(t => t.SlaPolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.Status, t.Priority });

        builder.HasIndex(t => new { t.AssigneeId, t.Status });

        builder.HasIndex(t => t.ResolutionDueAt)
            .HasFilter("[Status] IN (1, 2, 3)");
    }
}

