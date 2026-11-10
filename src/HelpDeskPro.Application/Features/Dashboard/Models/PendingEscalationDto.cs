namespace HelpDeskPro.Application.Features.Dashboard.Models;

using HelpDeskPro.Domain.Enums;

public sealed record PendingEscalationDto(
    long EscalationId,
    long TicketId,
    string TicketNo,
    string TicketTitle,
    TicketPriority Priority,
    byte Level,
    EscalationReason Reason,
    string? Note,
    DateTime EscalatedAt,
    string EscalatedToUserName,
    string? EscalatedByUserName
);

