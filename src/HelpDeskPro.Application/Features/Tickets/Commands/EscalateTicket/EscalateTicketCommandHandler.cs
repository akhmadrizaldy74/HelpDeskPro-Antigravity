namespace HelpDeskPro.Application.Features.Tickets.Commands.EscalateTicket;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Escalations.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class EscalateTicketCommandHandler : IRequestHandler<EscalateTicketCommand, EscalationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;

    public EscalateTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notificationService = notificationService;
    }

    public async Task<EscalationDto> Handle(EscalateTicketCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var role = _currentUserService.Role ?? "Requester";
        if (role == "Requester")
        {
            throw new BusinessRuleException("Requester tidak memiliki wewenang untuk melakukan eskalasi tiket.");
        }

        var ticket = await _context.Tickets
            .Include(t => t.Escalations)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
        {
            throw new NotFoundException("Tiket", request.TicketId);
        }

        var caller = await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        byte escalationLevel = role == "Supervisor" ? (byte)2 : (byte)1;
        ApplicationUser? recipient = null;

        if (caller?.SupervisorId != null)
        {
            recipient = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == caller.SupervisorId.Value, cancellationToken);
        }

        recipient ??= await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Department != null && u.Department.Code == "IT" && u.SupervisorId != null, cancellationToken)
            ?? await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Department != null && u.Department.Code == "IT" && u.Id != currentUserId, cancellationToken);

        if (recipient == null)
        {
            throw new BusinessRuleException("Tidak ditemukan supervisor penerima eskalasi di departemen IT.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var escalation = new Escalation
        {
            TicketId = ticket.Id,
            Level = escalationLevel,
            Reason = EscalationReason.Manual,
            Note = request.Note.Trim(),
            EscalatedById = currentUserId,
            EscalatedToId = recipient.Id,
            EscalatedAt = utcNow
        };

        ticket.Escalations.Add(escalation);
        ticket.EscalationLevel = Math.Max(ticket.EscalationLevel, escalationLevel);

        await _notificationService.CreateNotificationAsync(
            recipient.Id,
            ticket.Id,
            NotificationType.Escalation,
            $"Eskalasi Manual: Tiket {ticket.TicketNo} - {request.Note.Trim()}",
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new EscalationDto
        {
            Id = escalation.Id,
            TicketId = ticket.Id,
            Level = escalation.Level,
            Reason = escalation.Reason,
            Note = escalation.Note,
            EscalatedById = currentUserId,
            EscalatedByName = caller?.FullName ?? _currentUserService.UserName,
            EscalatedToId = recipient.Id,
            EscalatedToName = recipient.FullName,
            EscalatedAt = escalation.EscalatedAt,
            AcknowledgedAt = escalation.AcknowledgedAt
        };
    }
}

