namespace HelpDeskPro.Application.Features.Tickets.Commands.CreateTicket;

using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, TicketDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITicketNumberGenerator _ticketNumberGenerator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISlaCalculator _slaCalculator;

    public CreateTicketCommandHandler(
        IApplicationDbContext context,
        ITicketNumberGenerator ticketNumberGenerator,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ISlaCalculator slaCalculator)
    {
        _context = context;
        _ticketNumberGenerator = ticketNumberGenerator;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _slaCalculator = slaCalculator;
    }

    public async Task<TicketDetailDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Kategori", request.CategoryId);
        }

        var slaPolicy = await _context.SlaPolicies
            .FirstOrDefaultAsync(s => s.Priority == request.Priority, cancellationToken);

        if (slaPolicy == null)
        {
            throw new NotFoundException("SLA Policy", request.Priority);
        }

        Guid requesterId;
        string? requesterName = null;
        if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
        {
            requesterId = parsedUserId;
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == requesterId, cancellationToken);
            requesterName = user?.FullName;
        }
        else
        {
            throw new BusinessRuleException("User pembuat tiket tidak valid atau tidak terautentikasi.");
        }

        var ticketNo = await _ticketNumberGenerator.GenerateTicketNumberAsync(cancellationToken);
        var utcNow = _dateTimeProvider.UtcNow;

        var (responseDueAt, resolutionDueAt) = await _slaCalculator.CalculateDueDatesAsync(
            utcNow,
            slaPolicy.ResponseTimeMinutes,
            slaPolicy.ResolutionTimeMinutes,
            cancellationToken);

        var ticket = Ticket.Create(
            ticketNo,
            request.Title,
            request.Description,
            request.CategoryId,
            request.Priority,
            requesterId,
            slaPolicy,
            utcNow,
            _currentUserService.UserName ?? requesterName ?? requesterId.ToString(),
            responseDueAt,
            resolutionDueAt);

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);

        return new TicketDetailDto
        {
            Id = ticket.Id,
            TicketNo = ticket.TicketNo,
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            CategoryName = category.Name,
            Priority = ticket.Priority,
            Status = ticket.Status,
            RequesterId = ticket.RequesterId,
            RequesterName = requesterName ?? string.Empty,
            AssigneeId = ticket.AssigneeId,
            AssigneeName = null,
            ResponseDueAt = ticket.ResponseDueAt,
            ResolutionDueAt = ticket.ResolutionDueAt,
            FirstRespondedAt = ticket.FirstRespondedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            IsSlaBreached = ticket.IsSlaBreached,
            EscalationLevel = ticket.EscalationLevel,
            RowVersion = ticket.RowVersion,
            CreatedAt = ticket.CreatedAt,
            CreatedBy = ticket.CreatedBy
        };
    }
}
