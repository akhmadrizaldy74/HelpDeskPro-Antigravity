namespace HelpDeskPro.Application.Features.Tickets.Queries.GetTickets;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Enums;
using MediatR;

public record GetTicketsQuery : IRequest<PagedResult<TicketListItemDto>>
{
    public IReadOnlyList<TicketStatus>? Statuses { get; init; }
    public IReadOnlyList<TicketPriority>? Priorities { get; init; }
    public int? CategoryId { get; init; }
    public Guid? AssigneeId { get; init; }
    public bool? Unassigned { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

