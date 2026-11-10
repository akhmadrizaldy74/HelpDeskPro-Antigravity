namespace HelpDeskPro.Application.Features.Notifications.Commands.MarkNotificationsAsRead;

using MediatR;

public record MarkNotificationsAsReadCommand : IRequest<int>;

