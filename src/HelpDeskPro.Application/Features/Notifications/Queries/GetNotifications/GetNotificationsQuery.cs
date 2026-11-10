namespace HelpDeskPro.Application.Features.Notifications.Queries.GetNotifications;

using HelpDeskPro.Application.Features.Notifications.Models;
using MediatR;

public record GetNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;

