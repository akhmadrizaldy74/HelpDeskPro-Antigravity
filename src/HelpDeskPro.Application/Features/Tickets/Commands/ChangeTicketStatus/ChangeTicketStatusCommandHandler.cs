namespace HelpDeskPro.Application.Features.Tickets.Commands.ChangeTicketStatus;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Common;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class ChangeTicketStatusCommandHandler : IRequestHandler<ChangeTicketStatusCommand, TicketDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;
    private readonly ISlaCalculator _slaCalculator;

    public ChangeTicketStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notificationService,
        ISlaCalculator slaCalculator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notificationService = notificationService;
        _slaCalculator = slaCalculator;
    }

    public async Task<TicketDetailDto> Handle(ChangeTicketStatusCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var userRole = _currentUserService.Role ?? "Requester";

        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Requester)
            .Include(t => t.Assignee)
            .Include(t => t.Comments)
            .Include(t => t.Histories)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (ticket == null)
        {
            throw new NotFoundException("Tiket", request.Id);
        }

        // Requester can only access their own ticket (security 404 to avoid enumeration)
        if (userRole == "Requester" && ticket.RequesterId != currentUserId)
        {
            throw new NotFoundException("Tiket", request.Id);
        }

        // Optimistic concurrency check
        if (request.RowVersion.Length > 0 && ticket.RowVersion.Length > 0 && !request.RowVersion.SequenceEqual(ticket.RowVersion))
        {
            throw new DbUpdateConcurrencyException("Tiket sudah diubah pengguna lain, muat ulang halaman.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        DateTime? adjustedResolutionDueAt = null;
        int? workingPausedMinutes = null;

        if (ticket.Status == Domain.Enums.TicketStatus.Pending && request.Status != Domain.Enums.TicketStatus.Pending && ticket.PausedAt.HasValue)
        {
            var adjustment = await _slaCalculator.AdjustDueDatesForPausedDurationAsync(
                ticket.ResolutionDueAt,
                ticket.PausedAt.Value,
                utcNow,
                cancellationToken);
            adjustedResolutionDueAt = adjustment.NewResolutionDueAt;
            workingPausedMinutes = adjustment.WorkingPausedMinutes;
        }

        ticket.ChangeStatus(request.Status, request.Reason, currentUserId, userRole, utcNow, adjustedResolutionDueAt, workingPausedMinutes);

        await _context.SaveChangesAsync(cancellationToken);

        // FR-28: Notifikasi status tiket saya berubah
        if (currentUserId != ticket.RequesterId)
        {
            await _notificationService.CreateNotificationAsync(
                ticket.RequesterId,
                ticket.Id,
                Domain.Enums.NotificationType.StatusChanged,
                $"Status tiket {ticket.TicketNo} telah diubah menjadi {ticket.Status}.",
                cancellationToken);
        }

        if (ticket.AssigneeId.HasValue && currentUserId != ticket.AssigneeId.Value)
        {
            await _notificationService.CreateNotificationAsync(
                ticket.AssigneeId.Value,
                ticket.Id,
                Domain.Enums.NotificationType.StatusChanged,
                $"Status tiket {ticket.TicketNo} telah diubah menjadi {ticket.Status}.",
                cancellationToken);
        }

        return new TicketDetailDto
        {
            Id = ticket.Id,
            TicketNo = ticket.TicketNo,
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            CategoryName = ticket.Category?.Name ?? string.Empty,
            Priority = ticket.Priority,
            Status = ticket.Status,
            RequesterId = ticket.RequesterId,
            RequesterName = ticket.Requester?.FullName ?? string.Empty,
            AssigneeId = ticket.AssigneeId,
            AssigneeName = ticket.Assignee?.FullName,
            ResponseDueAt = ticket.ResponseDueAt,
            ResolutionDueAt = ticket.ResolutionDueAt,
            SlaRemainingMinutes = SlaCalculator.CalculateRemainingMinutes(
                ticket.ResolutionDueAt,
                ticket.Status,
                ticket.ResolvedAt,
                ticket.ClosedAt,
                ticket.PausedAt,
                utcNow),
            FirstRespondedAt = ticket.FirstRespondedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            PausedAt = ticket.PausedAt,
            TotalPausedMinutes = ticket.TotalPausedMinutes,
            IsSlaBreached = ticket.IsSlaBreached,
            EscalationLevel = ticket.EscalationLevel,
            RowVersion = ticket.RowVersion,
            CreatedAt = ticket.CreatedAt,
            CreatedBy = ticket.CreatedBy
        };
    }
}

