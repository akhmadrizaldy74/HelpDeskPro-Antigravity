namespace HelpDeskPro.Application.Features.Tickets.Commands.UpdateTicket;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Common;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UpdateTicketCommandHandler : IRequestHandler<UpdateTicketCommand, TicketDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<TicketDetailDto> Handle(UpdateTicketCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var role = _currentUserService.Role ?? "Requester";

        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Requester)
            .Include(t => t.Assignee)
            .Include(t => t.Histories)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (ticket == null)
        {
            throw new NotFoundException("Tiket", request.Id);
        }

        // Requester can only access their own ticket (security 404)
        if (role == "Requester" && ticket.RequesterId != currentUserId)
        {
            throw new NotFoundException("Tiket", request.Id);
        }

        // Supervisor department scoping
        if (role == "Supervisor")
        {
            var supervisor = await _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

            var isItSupervisor = supervisor?.Department?.Code == "IT";
            var isOwnDept = supervisor?.DepartmentId != null &&
                (ticket.Requester.DepartmentId == supervisor.DepartmentId ||
                 (ticket.Assignee != null && ticket.Assignee.DepartmentId == supervisor.DepartmentId));

            if (!isItSupervisor && !isOwnDept)
            {
                throw new NotFoundException("Tiket", request.Id);
            }
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Kategori", request.CategoryId);
        }

        // Optimistic concurrency check
        if (request.RowVersion.Length > 0 && ticket.RowVersion.Length > 0 && !request.RowVersion.SequenceEqual(ticket.RowVersion))
        {
            throw new DbUpdateConcurrencyException("Tiket sudah diubah pengguna lain, muat ulang halaman.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        ticket.UpdateDetails(
            request.Title,
            request.Description,
            category,
            request.Priority,
            currentUserId,
            role,
            utcNow);

        await _context.SaveChangesAsync(cancellationToken);

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

