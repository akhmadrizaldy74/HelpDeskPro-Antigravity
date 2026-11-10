namespace HelpDeskPro.Application.Features.Escalations.Commands.AcknowledgeEscalation;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Escalations.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class AcknowledgeEscalationCommandHandler : IRequestHandler<AcknowledgeEscalationCommand, EscalationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcknowledgeEscalationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EscalationDto> Handle(AcknowledgeEscalationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var escalation = await _context.Escalations
            .Include(e => e.EscalatedTo)
            .Include(e => e.EscalatedBy)
            .FirstOrDefaultAsync(e => e.Id == request.EscalationId, cancellationToken);

        if (escalation == null)
        {
            throw new NotFoundException("Eskalasi", request.EscalationId);
        }

        var role = _currentUserService.Role ?? "Requester";
        if (escalation.EscalatedToId != currentUserId && role != "Admin")
        {
            throw new BusinessRuleException("Hanya penerima eskalasi yang dapat menandai eskalasi sudah ditindaklanjuti.");
        }

        escalation.AcknowledgedAt = _dateTimeProvider.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new EscalationDto
        {
            Id = escalation.Id,
            TicketId = escalation.TicketId,
            Level = escalation.Level,
            Reason = escalation.Reason,
            Note = escalation.Note,
            EscalatedById = escalation.EscalatedById,
            EscalatedByName = escalation.EscalatedBy?.FullName,
            EscalatedToId = escalation.EscalatedToId,
            EscalatedToName = escalation.EscalatedTo.FullName,
            EscalatedAt = escalation.EscalatedAt,
            AcknowledgedAt = escalation.AcknowledgedAt
        };
    }
}

