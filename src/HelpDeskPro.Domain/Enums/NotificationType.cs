namespace HelpDeskPro.Domain.Enums;

public enum NotificationType : byte
{
    TicketAssigned = 1,
    NewComment = 2,
    StatusChanged = 3,
    Escalation = 4
}

