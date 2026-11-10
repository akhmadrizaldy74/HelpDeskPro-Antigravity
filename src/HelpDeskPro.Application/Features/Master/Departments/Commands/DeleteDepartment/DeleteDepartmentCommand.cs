namespace HelpDeskPro.Application.Features.Master.Departments.Commands.DeleteDepartment;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed record DeleteDepartmentCommand(int Id) : IRequest;

public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteDepartmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (department is null)
        {
            throw new NotFoundException("Departemen", request.Id);
        }

        var hasUsers = await _context.Users
            .AnyAsync(u => u.DepartmentId == request.Id, cancellationToken);

        if (hasUsers)
        {
            throw new BusinessRuleException("Departemen masih digunakan oleh pengguna dan tidak dapat dihapus. Silakan nonaktifkan departemen ini.");
        }

        var hasTickets = await _context.Tickets
            .AnyAsync(t => t.Requester.DepartmentId == request.Id || (t.Assignee != null && t.Assignee.DepartmentId == request.Id), cancellationToken);

        if (hasTickets)
        {
            throw new BusinessRuleException("Departemen masih digunakan oleh tiket dan tidak dapat dihapus. Silakan nonaktifkan departemen ini.");
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

