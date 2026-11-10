namespace HelpDeskPro.Domain.Enums;

public enum TicketStatus : byte
{
    Open = 1,
    InProgress = 2,
    Pending = 3,
    Resolved = 4,
    Closed = 5,
    Cancelled = 6
}

