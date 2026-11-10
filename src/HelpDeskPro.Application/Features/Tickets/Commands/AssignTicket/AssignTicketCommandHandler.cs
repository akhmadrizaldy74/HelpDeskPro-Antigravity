namespace HelpDeskPro.Application.Features.Tickets.Commands.AssignTicket;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Common;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, TicketDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;

    public AssignTicketCommandHandler(
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

    public async Task<TicketDetailDto> Handle(AssignTicketCommand request, CancellationToken cancellationToken)
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
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (ticket == null)
        {
            throw new NotFoundException("Tiket", request.Id);
        }

        var assignee = await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == request.AssigneeId, cancellationToken);

        if (assignee == null)
        {
            throw new NotFoundException("Pengguna", request.AssigneeId);
        }

        // FR-10: IT Department check for assignee
        if (assignee.Department?.Code != "IT")
        {
            throw new BusinessRuleException("Tiket hanya dapat ditugaskan kepada staf di departemen IT.");
        }

        // Optimistic concurrency check
        if (request.RowVersion.Length > 0 && ticket.RowVersion.Length > 0 && !request.RowVersion.SequenceEqual(ticket.RowVersion))
        {
            throw new DbUpdateConcurrencyException("Tiket sudah diubah pengguna lain, muat ulang halaman.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        ticket.Assign(assignee, currentUserId, userRole, utcNow);

        await _context.SaveChangesAsync(cancellationToken);

        // FR-28: Notifikasi tiket di-assign ke saya
        if (assignee.Id != currentUserId)
        {
            await _notificationService.CreateNotificationAsync(
                assignee.Id,
                ticket.Id,
                Domain.Enums.NotificationType.TicketAssigned,
                $"Tiket {ticket.TicketNo} telah ditugaskan kepada Anda.",
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
