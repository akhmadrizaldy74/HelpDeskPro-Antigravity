namespace HelpDeskPro.Application.Features.Master.Users.Commands.DeleteUser;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public sealed record DeleteUserCommand(Guid UserId) : IRequest;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DeleteUserCommandHandler(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Pengguna", request.UserId);
        }

        var hasTickets = await _context.Tickets
            .AnyAsync(t => t.RequesterId == request.UserId || t.AssigneeId == request.UserId, cancellationToken);

        if (hasTickets)
        {
            throw new BusinessRuleException("Pengguna masih memiliki riwayat tiket dan tidak dapat dihapus. Silakan nonaktifkan pengguna ini.");
        }

        var hasComments = await _context.TicketComments
            .AnyAsync(c => c.AuthorId == request.UserId, cancellationToken);

        if (hasComments)
        {
            throw new BusinessRuleException("Pengguna masih memiliki riwayat komentar dan tidak dapat dihapus. Silakan nonaktifkan pengguna ini.");
        }

        var hasSubordinates = await _context.Users
            .AnyAsync(u => u.SupervisorId == request.UserId, cancellationToken);

        if (hasSubordinates)
        {
            throw new BusinessRuleException("Pengguna masih memiliki bawahan (subordinat) dan tidak dapat dihapus. Silakan alihkan bawahan terlebih dahulu.");
        }

        var hasEscalations = await _context.Escalations
            .AnyAsync(e => e.EscalatedToId == request.UserId || e.EscalatedById == request.UserId, cancellationToken);

        if (hasEscalations)
        {
            throw new BusinessRuleException("Pengguna masih memiliki riwayat eskalasi dan tidak dapat dihapus. Silakan nonaktifkan pengguna ini.");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault()?.Description ?? "Gagal menghapus pengguna.";
            throw new BusinessRuleException(error);
        }
    }
}
