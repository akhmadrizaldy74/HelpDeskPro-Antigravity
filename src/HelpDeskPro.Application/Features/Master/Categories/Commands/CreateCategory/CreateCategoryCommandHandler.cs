namespace HelpDeskPro.Application.Features.Master.Categories.Commands.CreateCategory;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Categories.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();

        var exists = await _context.Categories
            .AnyAsync(c => c.Name == normalizedName, cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException($"Nama kategori '{normalizedName}' sudah digunakan.");
        }

        var category = new Category
        {
            Name = normalizedName,
            IsActive = true
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.IsActive, 0);
    }
}

