namespace HelpDeskPro.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

using HelpDeskPro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetUnreadNotificationCountQueryHandler : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadNotificationCountQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var count = await _context.Notifications
            .Where(n => n.UserId == currentUserId && !n.IsRead)
            .CountAsync(cancellationToken);

        return count;
    }
}

