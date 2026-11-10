namespace HelpDeskPro.Application.Features.Master.Categories.Commands.DeleteCategory;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed record DeleteCategoryCommand(int Id) : IRequest;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException("Kategori", request.Id);
        }

        var hasTickets = await _context.Tickets
            .AnyAsync(t => t.CategoryId == request.Id, cancellationToken);

        if (hasTickets)
        {
            throw new BusinessRuleException("Kategori masih digunakan oleh tiket dan tidak dapat dihapus. Silakan nonaktifkan kategori ini.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

