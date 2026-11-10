namespace HelpDeskPro.Application.Features.Sla.Commands.CheckSlaBreaches;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class CheckSlaBreachesCommandHandler : IRequestHandler<CheckSlaBreachesCommand, CheckSlaBreachesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CheckSlaBreachesCommandHandler> _logger;

    public CheckSlaBreachesCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        INotificationService notificationService,
        ILogger<CheckSlaBreachesCommandHandler> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<CheckSlaBreachesResult> Handle(CheckSlaBreachesCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var responseBreachesCount = 0;
        var resolutionBreachesCount = 0;
        var autoClosedCount = 0;

        // 1. Auto-close tickets resolved >= 3 days without response (FR-15)
        var autoCloseThreshold = utcNow.AddDays(-3);
        var resolvedTickets = await _context.Tickets
            .Include(t => t.Histories)
            .Where(t => t.Status == TicketStatus.Resolved && t.ResolvedAt != null && t.ResolvedAt <= autoCloseThreshold)
            .ToListAsync(cancellationToken);

        foreach (var ticket in resolvedTickets)
        {
            ticket.ChangeStatus(TicketStatus.Closed, "Auto-closed setelah 3 hari tanpa respons.", Guid.Empty, "System", utcNow);

            await _notificationService.CreateNotificationAsync(
                ticket.RequesterId,
                ticket.Id,
                NotificationType.StatusChanged,
                $"Tiket {ticket.TicketNo} telah otomatis ditutup setelah 3 hari tanpa respons.",
                cancellationToken);

            autoClosedCount++;
        }

        // 2. Query active tickets (Open, InProgress, Pending) with their relationships
        var activeTickets = await _context.Tickets
            .Include(t => t.Assignee)
            .Include(t => t.Escalations)
            .Where(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var ticket in activeTickets)
        {
            // A. Check Response SLA Breach (FR-22, AC-04)
            // now > ResponseDueAt and FirstRespondedAt is empty
            if (ticket.FirstRespondedAt == null && utcNow > ticket.ResponseDueAt)
            {
                var alreadyEscalatedLevel1 = ticket.Escalations.Any(e =>
                    e.Level == 1 && e.Reason == EscalationReason.SlaResponseBreach);

                // FR-24: Idempotency check
                if (!alreadyEscalatedLevel1)
                {
                    var level1Supervisor = await GetLevel1SupervisorAsync(ticket, cancellationToken);
                    if (level1Supervisor != null)
                    {
                        var escalation = new Escalation
                        {
                            TicketId = ticket.Id,
                            Level = 1,
                            Reason = EscalationReason.SlaResponseBreach,
                            EscalatedToId = level1Supervisor.Id,
                            EscalatedById = null,
                            EscalatedAt = utcNow
                        };

                        ticket.Escalations.Add(escalation);
                        ticket.EscalationLevel = Math.Max(ticket.EscalationLevel, (byte)1);

                        await _notificationService.CreateNotificationAsync(
                            level1Supervisor.Id,
                            ticket.Id,
                            NotificationType.Escalation,
                            $"Eskalasi Level 1: Tiket {ticket.TicketNo} belum direspons dalam batas waktu SLA.",
                            cancellationToken);

                        responseBreachesCount++;
                    }
                    else
                    {
                        _logger.LogWarning("Penerima eskalasi Level 1 untuk tiket {TicketNo} tidak ditemukan.", ticket.TicketNo);
                    }
                }
            }

            // B. Check Resolution SLA Breach (FR-23, FR-27)
            // FR-27: Selama status Pending, waktu SLA resolusi tidak berjalan (tiket Pending tidak dihitung)
            if (ticket.Status != TicketStatus.Pending && utcNow > ticket.ResolutionDueAt)
            {
                ticket.IsSlaBreached = true;

                var alreadyEscalatedLevel2 = ticket.Escalations.Any(e =>
                    e.Level == 2 && e.Reason == EscalationReason.SlaResolutionBreach);

                // FR-24: Idempotency check
                if (!alreadyEscalatedLevel2)
                {
                    var level1Supervisor = await GetLevel1SupervisorAsync(ticket, cancellationToken);
                    var level2Supervisor = await GetLevel2SupervisorAsync(level1Supervisor, cancellationToken);

                    if (level2Supervisor != null)
                    {
                        var escalation = new Escalation
                        {
                            TicketId = ticket.Id,
                            Level = 2,
                            Reason = EscalationReason.SlaResolutionBreach,
                            EscalatedToId = level2Supervisor.Id,
                            EscalatedById = null,
                            EscalatedAt = utcNow
                        };

                        ticket.Escalations.Add(escalation);
                        ticket.EscalationLevel = Math.Max(ticket.EscalationLevel, (byte)2);

                        await _notificationService.CreateNotificationAsync(
                            level2Supervisor.Id,
                            ticket.Id,
                            NotificationType.Escalation,
                            $"Eskalasi Level 2: Tiket {ticket.TicketNo} belum diselesaikan dalam batas waktu SLA.",
                            cancellationToken);

                        resolutionBreachesCount++;
                    }
                    else
                    {
                        _logger.LogWarning("Penerima eskalasi Level 2 untuk tiket {TicketNo} tidak ditemukan.", ticket.TicketNo);
                    }
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new CheckSlaBreachesResult(responseBreachesCount, resolutionBreachesCount, autoClosedCount);
    }

    private async Task<ApplicationUser?> GetLevel1SupervisorAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        // 1. If ticket is assigned and assignee has a direct supervisor
        if (ticket.Assignee != null && ticket.Assignee.SupervisorId.HasValue)
        {
            var directSupervisor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == ticket.Assignee.SupervisorId.Value, cancellationToken);

            if (directSupervisor != null)
            {
                return directSupervisor;
            }
        }

        // 2. If unassigned or direct supervisor not found: find Supervisor in IT Department (excluding assignee)
        var itDeptSupervisor = await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Department != null && u.Department.Code == "IT" && (ticket.AssigneeId == null || u.Id != ticket.AssigneeId.Value) && u.SupervisorId != null, cancellationToken)
            ?? await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Department != null && u.Department.Code == "IT" && (ticket.AssigneeId == null || u.Id != ticket.AssigneeId.Value), cancellationToken);

        return itDeptSupervisor;
    }

    private async Task<ApplicationUser?> GetLevel2SupervisorAsync(ApplicationUser? level1Supervisor, CancellationToken cancellationToken)
    {
        if (level1Supervisor?.SupervisorId != null)
        {
            var level2Supervisor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == level1Supervisor.SupervisorId.Value, cancellationToken);

            return level2Supervisor;
        }

        return null;
    }
}
