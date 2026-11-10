namespace HelpDeskPro.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

using MediatR;

public record GetUnreadNotificationCountQuery : IRequest<int>;

