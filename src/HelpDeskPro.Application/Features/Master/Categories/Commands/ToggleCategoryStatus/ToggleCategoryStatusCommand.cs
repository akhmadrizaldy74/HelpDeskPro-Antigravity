namespace HelpDeskPro.Application.Features.Master.Categories.Commands.ToggleCategoryStatus;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Categories.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed record ToggleCategoryStatusCommand(int Id, bool? IsActive = null) : IRequest<CategoryDto>;

public class ToggleCategoryStatusCommandHandler : IRequestHandler<ToggleCategoryStatusCommand, CategoryDto>
{
    private readonly IApplicationDbContext _context;

    public ToggleCategoryStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto> Handle(ToggleCategoryStatusCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .Include(c => c.Tickets)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException("Kategori", request.Id);
        }

        category.IsActive = request.IsActive ?? !category.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.IsActive, category.Tickets.Count);
    }
}

