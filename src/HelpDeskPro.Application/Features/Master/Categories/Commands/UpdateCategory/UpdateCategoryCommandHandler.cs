namespace HelpDeskPro.Application.Features.Master.Categories.Commands.UpdateCategory;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Categories.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .Include(c => c.Tickets)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException("Kategori", request.Id);
        }

        var normalizedName = request.Name.Trim();

        var exists = await _context.Categories
            .AnyAsync(c => c.Name == normalizedName && c.Id != request.Id, cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException($"Nama kategori '{normalizedName}' sudah digunakan.");
        }

        category.Name = normalizedName;
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.IsActive, category.Tickets.Count);
    }
}

