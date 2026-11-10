namespace HelpDeskPro.Application.Common.Interfaces;

using HelpDeskPro.Domain.Enums;

public interface INotificationService
{
    Task CreateNotificationAsync(
        Guid userId,
        long? ticketId,
        NotificationType type,
        string message,
        CancellationToken cancellationToken = default);
}

