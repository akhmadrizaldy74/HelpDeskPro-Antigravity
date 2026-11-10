namespace HelpDeskPro.Application.Common.Interfaces;

using HelpDeskPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface IApplicationDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<Category> Categories { get; }
    DbSet<SlaPolicy> SlaPolicies { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<TicketAttachment> TicketAttachments { get; }
    DbSet<TicketHistory> TicketHistories { get; }
    DbSet<Escalation> Escalations { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<TicketNumberSequence> TicketNumberSequences { get; }
    DbSet<Holiday> Holidays { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
